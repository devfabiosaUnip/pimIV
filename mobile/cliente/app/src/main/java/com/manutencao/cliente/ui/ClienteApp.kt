package com.manutencao.cliente.ui

import android.Manifest
import android.annotation.SuppressLint
import android.content.pm.PackageManager
import android.location.Location
import android.location.LocationListener
import android.location.LocationManager
import android.os.Looper
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
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableDoubleStateOf
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
import coil.compose.AsyncImage
import com.manutencao.cliente.data.ApiService
import com.manutencao.cliente.data.ClientRequestDetail
import com.manutencao.cliente.data.CreateServiceRequest
import com.manutencao.cliente.data.LoginRequest
import com.manutencao.cliente.data.ServiceRequestSummary
import com.manutencao.cliente.data.SubmitFeedback
import com.manutencao.cliente.data.TechnicianTracking
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

@Composable
fun ClienteApp() {
    var token by remember { mutableStateOf<String?>(null) }
    var userName by remember { mutableStateOf("Cliente") }
    val api = remember { ApiService.create { token } }
    var screen by remember { mutableStateOf("home") }
    var selectedId by remember { mutableStateOf<String?>(null) }
    var requests by remember { mutableStateOf(emptyList<ServiceRequestSummary>()) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    suspend fun reload() {
        runCatching {
            requests = api.myRequests()
            error = null
        }.onFailure {
            error = it.message ?: "Não foi possível carregar os chamados."
        }
    }

    ClienteTheme {
        Surface(Modifier.fillMaxSize()) {
            when {
                token == null -> LoginPane(error) { email, password ->
                    scope.launch {
                        runCatching {
                            val res = api.login(LoginRequest(email, password))
                            token = res.token
                            userName = res.fullName ?: "Cliente"
                            reload()
                            screen = "home"
                        }.onFailure {
                            error = it.message ?: "Não foi possível entrar."
                        }
                    }
                }

                screen == "new" -> NewRequestPane(
                    onBack = { screen = "home" },
                    onSubmit = { body ->
                        scope.launch {
                            runCatching {
                                api.create(body)
                                reload()
                                screen = "home"
                            }.onFailure {
                                error = it.message ?: "Não foi possível criar o chamado."
                            }
                        }
                    }
                )

                screen == "detail" && selectedId != null -> RequestDetailPane(
                    api = api,
                    requestId = selectedId!!,
                    onBack = {
                        screen = "home"
                        scope.launch { reload() }
                    }
                )

                else -> HomePane(
                    userName = userName,
                    requests = requests,
                    error = error,
                    onNew = { screen = "new" },
                    onOpen = { id ->
                        selectedId = id
                        error = null
                        screen = "detail"
                    },
                    onRefresh = { scope.launch { reload() } }
                )
            }
        }
    }
}

@Composable
private fun LoginPane(
    error: String?,
    onLogin: (String, String) -> Unit
) {
    var email by remember { mutableStateOf("cliente@demo.local") }
    var password by remember { mutableStateOf("Cliente@123") }

    Column(
        Modifier.fillMaxSize().padding(24.dp),
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            "Cliente",
            style = MaterialTheme.typography.labelLarge,
            color = MaterialTheme.colorScheme.primary
        )
        Text(
            "Manutenção",
            style = MaterialTheme.typography.headlineLarge,
            fontWeight = FontWeight.Bold
        )
        Text(
            "Acompanhe o técnico até a sua porta.",
            color = MaterialTheme.colorScheme.primary
        )
        Spacer(Modifier.height(24.dp))
        OutlinedTextField(
            value = email,
            onValueChange = { email = it },
            label = { Text("E-mail") },
            modifier = Modifier.fillMaxWidth()
        )
        OutlinedTextField(
            value = password,
            onValueChange = { password = it },
            label = { Text("Senha") },
            modifier = Modifier.fillMaxWidth()
        )
        error?.let {
            Text(
                it,
                color = MaterialTheme.colorScheme.error,
                modifier = Modifier.padding(top = 8.dp)
            )
        }
        Button(
            onClick = { onLogin(email, password) },
            modifier = Modifier.fillMaxWidth().padding(top = 16.dp)
        ) {
            Text("Entrar")
        }
    }
}

@Composable
private fun HomePane(
    userName: String,
    requests: List<ServiceRequestSummary>,
    error: String?,
    onNew: () -> Unit,
    onOpen: (String) -> Unit,
    onRefresh: () -> Unit
) {
    Column(Modifier.fillMaxSize().padding(16.dp)) {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column {
                Text(
                    "Olá, $userName",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold
                )
                Text("Seus chamados", color = MaterialTheme.colorScheme.primary)
            }
            TextButton(onClick = onRefresh) {
                Text("Atualizar")
            }
        }

        error?.let {
            Text(it, color = MaterialTheme.colorScheme.error)
        }

        Button(
            onClick = onNew,
            modifier = Modifier.fillMaxWidth().padding(vertical = 12.dp)
        ) {
            Text("Nova requisição")
        }

        if (requests.isEmpty()) {
            Text(
                "Nenhum chamado ainda. Abra uma requisição para a central destinar um técnico.",
                modifier = Modifier.padding(top = 12.dp)
            )
        }

        LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
            items(requests, key = { it.id }) { request ->
                Card(
                    Modifier
                        .fillMaxWidth()
                        .clickable { onOpen(request.id) }
                ) {
                    Column(Modifier.padding(16.dp)) {
                        Text(
                            "${request.protocol} · ${statusLabel(request.status)}",
                            color = MaterialTheme.colorScheme.primary
                        )
                        Text(
                            request.title,
                            fontWeight = FontWeight.SemiBold
                        )
                        Text(request.description, maxLines = 2)
                    }
                }
            }
        }
    }
}

@SuppressLint("MissingPermission")
@Composable
private fun NewRequestPane(
    onBack: () -> Unit,
    onSubmit: (CreateServiceRequest) -> Unit
) {
    val context = LocalContext.current
    var title by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var category by remember { mutableStateOf("Elétrica") }
    var address by remember { mutableStateOf("") }
    var lat by remember { mutableDoubleStateOf(-23.5505) }
    var lng by remember { mutableDoubleStateOf(-46.6333) }
    var gpsLabel by remember { mutableStateOf("Toque para usar o GPS do aparelho") }
    var gpsReady by remember { mutableStateOf(false) }

    fun applyLocation(location: Location) {
        lat = location.latitude
        lng = location.longitude
        gpsReady = true
        gpsLabel = "GPS: ${"%.5f".format(lat)}, ${"%.5f".format(lng)}"
    }

    fun captureGps() {
        val locationManager =
            context.getSystemService(LocationManager::class.java)

        val last = runCatching {
            locationManager.getLastKnownLocation(LocationManager.GPS_PROVIDER)
        }.getOrNull() ?: runCatching {
            locationManager.getLastKnownLocation(LocationManager.NETWORK_PROVIDER)
        }.getOrNull()

        last?.let { applyLocation(it) }

        val listener = object : LocationListener {
            override fun onLocationChanged(location: Location) {
                applyLocation(location)
                runCatching { locationManager.removeUpdates(this) }
            }
        }

        listOf(
            LocationManager.GPS_PROVIDER,
            LocationManager.NETWORK_PROVIDER
        ).forEach { provider ->
            runCatching {
                if (locationManager.isProviderEnabled(provider)) {
                    locationManager.requestLocationUpdates(
                        provider,
                        0L,
                        0f,
                        listener,
                        Looper.getMainLooper()
                    )
                }
            }
        }

        gpsLabel = "Obtendo GPS..."
    }

    val permission = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        if (granted) {
            captureGps()
        } else {
            gpsLabel = "Permissão de GPS negada"
        }
    }

    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp)
    ) {
        TextButton(onClick = onBack) {
            Text("← Voltar")
        }
        Text(
            "Abrir chamado",
            style = MaterialTheme.typography.headlineSmall,
            fontWeight = FontWeight.Bold
        )
        OutlinedTextField(
            value = title,
            onValueChange = { title = it },
            label = { Text("Título") },
            modifier = Modifier.fillMaxWidth()
        )
        OutlinedTextField(
            value = description,
            onValueChange = { description = it },
            label = { Text("Descrição do problema") },
            modifier = Modifier.fillMaxWidth().height(120.dp)
        )
        OutlinedTextField(
            value = category,
            onValueChange = { category = it },
            label = { Text("Categoria") },
            modifier = Modifier.fillMaxWidth()
        )
        OutlinedTextField(
            value = address,
            onValueChange = { address = it },
            label = { Text("Endereço") },
            modifier = Modifier.fillMaxWidth()
        )

        TextButton(onClick = {
            if (
                ContextCompat.checkSelfPermission(
                    context,
                    Manifest.permission.ACCESS_FINE_LOCATION
                ) == PackageManager.PERMISSION_GRANTED
            ) {
                captureGps()
            } else {
                permission.launch(Manifest.permission.ACCESS_FINE_LOCATION)
            }
        }) {
            Text(gpsLabel)
        }

        Button(
            enabled = title.isNotBlank() &&
                description.isNotBlank() &&
                address.isNotBlank(),
            onClick = {
                onSubmit(
                    CreateServiceRequest(
                        title,
                        description,
                        category,
                        1,
                        address,
                        "São Paulo",
                        "SP",
                        "01000-000",
                        lat,
                        lng
                    )
                )
            },
            modifier = Modifier.fillMaxWidth().padding(top = 12.dp)
        ) {
            Text("Enviar requisição")
        }

        if (!gpsReady) {
            Text(
                "Use o GPS para o técnico encontrar o local com precisão.",
                style = MaterialTheme.typography.bodySmall
            )
        }
    }
}

@Composable
private fun RequestDetailPane(
    api: ApiService,
    requestId: String,
    onBack: () -> Unit
) {
    var detail by remember(requestId) {
        mutableStateOf<ClientRequestDetail?>(null)
    }
    var tracking by remember(requestId) {
        mutableStateOf<TechnicianTracking?>(null)
    }
    var comment by remember(requestId) { mutableStateOf("") }
    var rating by remember(requestId) { mutableIntStateOf(5) }
    var sent by remember(requestId) { mutableStateOf(false) }
    var error by remember(requestId) { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(requestId) {
        while (true) {
            runCatching {
                detail = api.detail(requestId)
                tracking = api.tracking(requestId)
                error = null
            }.onFailure {
                error = it.message ?: "Não foi possível carregar o chamado."
            }
            delay(3000)
        }
    }

    val currentDetail = detail

    Column(
        Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp)
    ) {
        TextButton(onClick = onBack) {
            Text("← Voltar")
        }

        if (currentDetail == null) {
            Text("Carregando o chamado...")
            error?.let {
                Text(it, color = MaterialTheme.colorScheme.error)
            }
        } else {
            val d = currentDetail

            Text(d.protocol, color = MaterialTheme.colorScheme.primary)
            Text(
                d.title,
                style = MaterialTheme.typography.headlineSmall,
                fontWeight = FontWeight.Bold
            )
            Text(
                statusLabel(d.status),
                modifier = Modifier.padding(vertical = 4.dp),
                fontWeight = FontWeight.SemiBold
            )
            Text(d.description)
            Text(
                d.addressLine,
                color = MaterialTheme.colorScheme.primary,
                modifier = Modifier.padding(top = 8.dp)
            )

            error?.let {
                Text(it, color = MaterialTheme.colorScheme.error)
            }

            val trackable =
                tracking?.trackingAvailable == true ||
                    d.status in listOf(
                        "EmDeslocamento",
                        "Atribuida",
                        "EmAtendimento"
                    )

            if (trackable) {
                Spacer(Modifier.height(12.dp))
                Text("Rastrear técnico", fontWeight = FontWeight.SemiBold)

                val name = tracking?.technicianName ?: d.technicianName ?: "Técnico"
                val km = tracking?.distanceKm
                val eta = tracking?.etaMinutes

                val summary = buildString {
                    append(name)
                    if (km != null) append(" · ${"%.1f".format(km)} km")
                    if (eta != null) append(" · ~${eta.toInt()} min")
                    if (tracking?.technicianLatitude == null) {
                        append(" · aguardando GPS")
                    }
                }

                Text(summary)

                // MAPA DESATIVADO TEMPORARIAMENTE PARA INVESTIGAR O CRASH.
                Text(
                    "Mapa temporariamente indisponível durante o teste.",
                    style = MaterialTheme.typography.bodySmall,
                    modifier = Modifier.padding(top = 8.dp)
                )
            }

            if (d.status == "Concluida") {
                Spacer(Modifier.height(16.dp))
                Text("Prova do reparo", fontWeight = FontWeight.SemiBold)

                d.proofPhotoUrl?.let { photoUrl ->
                    AsyncImage(
                        model = photoUrl,
                        contentDescription = "Foto do reparo",
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(220.dp)
                            .clip(RoundedCornerShape(16.dp))
                            .padding(vertical = 8.dp),
                        contentScale = ContentScale.Crop
                    )
                } ?: Text(
                    "Foto ainda não disponível.",
                    color = MaterialTheme.colorScheme.primary
                )

                d.completionNotes?.let { Text(it) }

                Spacer(Modifier.height(12.dp))

                if (d.feedbackRating == null && !sent) {
                    Text("Como foi o atendimento?", fontWeight = FontWeight.SemiBold)

                    Row {
                        (1..5).forEach { star ->
                            TextButton(onClick = { rating = star }) {
                                Text(
                                    if (star <= rating) "★" else "☆",
                                    style = MaterialTheme.typography.headlineSmall
                                )
                            }
                        }
                    }

                    OutlinedTextField(
                        value = comment,
                        onValueChange = { comment = it },
                        label = { Text("Comentário (opcional)") },
                        modifier = Modifier.fillMaxWidth()
                    )

                    Button(
                        onClick = {
                            scope.launch {
                                runCatching {
                                    api.feedback(
                                        requestId,
                                        SubmitFeedback(rating, comment.ifBlank { null })
                                    )
                                    detail = api.detail(requestId)
                                    sent = true
                                }.onFailure {
                                    error = it.message ?: "Não foi possível enviar o feedback."
                                }
                            }
                        },
                        modifier = Modifier.fillMaxWidth().padding(top = 8.dp)
                    ) {
                        Text("Enviar feedback")
                    }
                } else {
                    val stars = d.feedbackRating ?: rating
                    Text("Sua avaliação: $stars/5", fontWeight = FontWeight.SemiBold)
                    (d.feedbackComment ?: comment.takeIf { it.isNotBlank() })?.let {
                        Text(it)
                    }
                }
            }
        }
    }
}