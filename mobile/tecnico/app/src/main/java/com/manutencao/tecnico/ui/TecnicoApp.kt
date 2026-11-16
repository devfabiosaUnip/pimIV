package com.manutencao.tecnico.ui

import android.Manifest
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.core.content.ContextCompat
import androidx.core.content.FileProvider
import coil.compose.AsyncImage
import com.manutencao.tecnico.data.ApiService
import com.manutencao.tecnico.data.LoginRequest
import com.manutencao.tecnico.data.SessionStore
import com.manutencao.tecnico.data.StatusUpdate
import com.manutencao.tecnico.data.TechnicianVisitDto
import com.manutencao.tecnico.gps.NavigationHelper
import com.manutencao.tecnico.gps.TrackingService
import com.manutencao.tecnico.vpn.VpnHelper
import kotlinx.coroutines.launch
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.asRequestBody
import okhttp3.RequestBody.Companion.toRequestBody
import java.io.File

@Composable
fun TecnicoApp() {
    val context = LocalContext.current
    var token by remember { mutableStateOf(SessionStore.token(context)) }
    val api = remember { ApiService.create { token } }
    var visits by remember { mutableStateOf<List<TechnicianVisitDto>>(emptyList()) }
    var selected by remember { mutableStateOf<TechnicianVisitDto?>(null) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    suspend fun reload() {
        visits = api.visits()
        selected = selected?.let { current -> visits.firstOrNull { it.assignmentId == current.assignmentId } }
    }

    TecnicoTheme {
        Surface(Modifier.fillMaxSize()) {
            if (token == null) {
                LoginScreen(error) { email, password ->
                    scope.launch {
                        runCatching {
                            val res = api.login(LoginRequest(email, password))
                            SessionStore.saveToken(context, res.token)
                            token = res.token
                            reload()
                        }.onFailure { error = it.message }
                    }
                }
            } else {
                val current = selected
                if (current == null) {
                    VisitList(
                        visits = visits,
                        error = error,
                        onSelect = { selected = it },
                        onRefresh = { scope.launch { runCatching { reload() }.onFailure { error = it.message } } },
                    )
                } else {
                    VisitDetail(
                        visit = current,
                        api = api,
                        error = error,
                        onError = { error = it },
                        onBack = { selected = null },
                        onReload = { scope.launch { runCatching { reload() }.onFailure { error = it.message } } }
                    )
                }
            }
        }
    }
}

@Composable
private fun LoginScreen(error: String?, onLogin: (String, String) -> Unit) {
    var email by remember { mutableStateOf("tecnico@empresa.local") }
    var password by remember { mutableStateOf("Tecnico@123") }
    Column(Modifier.fillMaxSize().padding(24.dp), verticalArrangement = Arrangement.Center) {
        Text("Campo", style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.primary)
        Text("App do técnico", style = MaterialTheme.typography.headlineLarge, fontWeight = FontWeight.Bold)
        Text("Abra o GPS, atenda e fotografe o reparo.", color = MaterialTheme.colorScheme.primary)
        Spacer(Modifier.height(20.dp))
        OutlinedTextField(email, { email = it }, label = { Text("E-mail") }, modifier = Modifier.fillMaxWidth())
        OutlinedTextField(password, { password = it }, label = { Text("Senha") }, modifier = Modifier.fillMaxWidth())
        error?.let { Text(it, color = MaterialTheme.colorScheme.error, modifier = Modifier.padding(top = 8.dp)) }
        Button(onClick = { onLogin(email, password) }, modifier = Modifier.fillMaxWidth().padding(top = 16.dp)) {
            Text("Entrar")
        }
    }
}

@Composable
private fun VisitList(
    visits: List<TechnicianVisitDto>,
    error: String?,
    onSelect: (TechnicianVisitDto) -> Unit,
    onRefresh: () -> Unit
) {
    val context = LocalContext.current
    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween, verticalAlignment = Alignment.CenterVertically) {
            Column {
                Text("Minhas visitas", style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
                Text("GPS, atendimento e prova fotográfica", color = MaterialTheme.colorScheme.primary)
            }
            TextButton(onClick = { runCatching { VpnHelper.openWireGuardImport(context) } }) { Text("VPN") }
        }
        TextButton(onClick = onRefresh) { Text("Atualizar") }
        error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        if (visits.isEmpty()) {
            Text("Nenhuma visita atribuída.", color = MaterialTheme.colorScheme.primary, modifier = Modifier.padding(top = 24.dp))
        }
        LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
            items(visits, key = { it.assignmentId }) { visit ->
                Card(Modifier.fillMaxWidth().clickable { onSelect(visit) }) {
                    Column(Modifier.padding(16.dp)) {
                        Text("${visit.protocol} · ${statusLabel(visit.visitStatus)}", color = MaterialTheme.colorScheme.primary)
                        Text(visit.title, fontWeight = FontWeight.SemiBold)
                        Text("${visit.addressLine}, ${visit.city}")
                    }
                }
            }
        }
    }
}

@Composable
private fun VisitDetail(
    visit: TechnicianVisitDto,
    api: ApiService,
    error: String?,
    onError: (String?) -> Unit,
    onBack: () -> Unit,
    onReload: () -> Unit
) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    var notes by remember { mutableStateOf("") }
    var photoUri by remember { mutableStateOf<Uri?>(null) }
    var photoFile by remember { mutableStateOf<File?>(null) }
    var tracking by remember { mutableStateOf(SessionStore.assignmentId(context) == visit.assignmentId) }

    fun preparePhotoFile(): Pair<File, Uri> {
        val dir = File(context.cacheDir, "photos").apply { mkdirs() }
        val file = File(dir, "prova-${visit.assignmentId}-${System.currentTimeMillis()}.jpg")
        val uri = FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", file)
        return file to uri
    }

    val photoLauncher = rememberLauncherForActivityResult(ActivityResultContracts.TakePicture()) { ok ->
        if (!ok) {
            photoUri = null
            photoFile = null
        }
    }
    val galleryLauncher = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri ->
        if (uri == null) return@rememberLauncherForActivityResult
        val (file, localUri) = preparePhotoFile()
        context.contentResolver.openInputStream(uri)?.use { input ->
            file.outputStream().use { input.copyTo(it) }
        }
        photoFile = file
        photoUri = localUri
    }
    val permissionLauncher = rememberLauncherForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { grants ->
        val locOk = grants[Manifest.permission.ACCESS_FINE_LOCATION] == true ||
            ContextCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED
        if (locOk) {
            tracking = true
            TrackingService.start(context, visit.assignmentId)
            scope.launch {
                runCatching { api.updateStatus(visit.assignmentId, StatusUpdate("EmDeslocamento")) }
                onReload()
            }
            NavigationHelper.openGps(context, visit.latitude, visit.longitude, visit.title)
        } else onError("Permita o GPS para rastrear o deslocamento.")
    }

    fun openGpsAndTrack() {
        val needed = mutableListOf(Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION)
        if (Build.VERSION.SDK_INT >= 33) needed += Manifest.permission.POST_NOTIFICATIONS
        val missing = needed.filter {
            ContextCompat.checkSelfPermission(context, it) != PackageManager.PERMISSION_GRANTED
        }
        if (missing.any { it.contains("LOCATION") }) {
            permissionLauncher.launch(needed.toTypedArray())
            return
        }
        tracking = true
        TrackingService.start(context, visit.assignmentId)
        scope.launch {
            runCatching { api.updateStatus(visit.assignmentId, StatusUpdate("EmDeslocamento")) }
            onReload()
        }
        NavigationHelper.openGps(context, visit.latitude, visit.longitude, visit.title)
    }

    val cameraPermission = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
        if (granted) {
            val (file, uri) = preparePhotoFile()
            photoFile = file
            photoUri = uri
            photoLauncher.launch(uri)
        } else onError("Permita a câmera para registrar a prova do reparo.")
    }

    fun takePhoto() {
        if (ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA) == PackageManager.PERMISSION_GRANTED) {
            val (file, uri) = preparePhotoFile()
            photoFile = file
            photoUri = uri
            photoLauncher.launch(uri)
        } else cameraPermission.launch(Manifest.permission.CAMERA)
    }

    Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp)) {
        TextButton(onClick = onBack) { Text("← Visitas") }
        Text(visit.protocol, color = MaterialTheme.colorScheme.primary)
        Text(visit.title, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
        Text(statusLabel(visit.visitStatus), color = MaterialTheme.colorScheme.secondary)
        Text(visit.description, modifier = Modifier.padding(vertical = 8.dp))
        Text("${visit.addressLine}, ${visit.city}")
        error?.let { Text(it, color = MaterialTheme.colorScheme.error) }

        LeafletMap(visit.latitude, visit.longitude, Modifier.fillMaxWidth().height(220.dp).clip(RoundedCornerShape(16.dp)).padding(vertical = 12.dp))

        Button(onClick = { openGpsAndTrack() }, modifier = Modifier.fillMaxWidth()) {
            Text(if (tracking) "GPS aberto · rastreando no mapa do cliente" else "Abrir GPS e ir até o cliente")
        }
        Text(
            "O rastreio continua em segundo plano enquanto você navega.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.primary,
            modifier = Modifier.padding(top = 6.dp, bottom = 8.dp)
        )

        if (visit.visitStatus != "Concluida") {
            OutlinedButton(
                onClick = {
                    scope.launch {
                        runCatching {
                            api.updateStatus(visit.assignmentId, StatusUpdate("EmAtendimento"))
                            onReload()
                        }.onFailure { onError(it.message) }
                    }
                },
                modifier = Modifier.fillMaxWidth()
            ) { Text("Cheguei no local") }

            Spacer(Modifier.height(16.dp))
            Text("1. Fotografe o reparo", fontWeight = FontWeight.SemiBold)
            Text("A foto fica anexada ao chamado como prova para o cliente e a central.")
            photoUri?.let {
                AsyncImage(
                    it,
                    contentDescription = "Prévia da prova",
                    modifier = Modifier.fillMaxWidth().height(220.dp).clip(RoundedCornerShape(12.dp)).padding(vertical = 8.dp),
                    contentScale = ContentScale.Crop
                )
            }
            Button(onClick = { takePhoto() }, modifier = Modifier.fillMaxWidth()) {
                Text(if (photoUri == null) "Fotografar reparo" else "Fotografar de novo")
            }
            TextButton(onClick = { galleryLauncher.launch("image/*") }, modifier = Modifier.fillMaxWidth()) {
                Text("Anexar da galeria")
            }
            OutlinedTextField(notes, { notes = it }, label = { Text("Observações do serviço") }, modifier = Modifier.fillMaxWidth())
            Button(
                enabled = photoFile?.exists() == true && photoFile?.length() ?: 0L > 0L,
                onClick = {
                    val file = photoFile ?: return@Button
                    scope.launch {
                        runCatching {
                            val part = MultipartBody.Part.createFormData(
                                "photo",
                                file.name,
                                file.asRequestBody("image/jpeg".toMediaType())
                            )
                            api.complete(
                                visit.assignmentId,
                                part,
                                notes.toRequestBody("text/plain".toMediaType())
                            )
                            TrackingService.stop(context)
                            tracking = false
                            onReload()
                            onBack()
                        }.onFailure { onError(it.message) }
                    }
                },
                modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp)
            ) { Text("Concluir serviço com foto") }
        }
    }
}
