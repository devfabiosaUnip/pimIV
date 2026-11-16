package com.manutencao.tecnico.gps

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.IBinder
import androidx.core.app.NotificationCompat
import com.manutencao.tecnico.BuildConfig
import com.manutencao.tecnico.data.SessionStore
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.util.concurrent.Executors

class TrackingService : Service() {
    private var reporter: LocationReporter? = null
    private val http = OkHttpClient()
    private val io = Executors.newSingleThreadExecutor()

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        if (intent?.action == ACTION_STOP) {
            tearDown()
            stopSelf()
            return START_NOT_STICKY
        }

        val assignmentId = intent?.getStringExtra(EXTRA_ASSIGNMENT) ?: SessionStore.assignmentId(this)
        val token = SessionStore.token(this)
        if (assignmentId.isNullOrBlank() || token.isNullOrBlank()) {
            stopSelf()
            return START_NOT_STICKY
        }

        SessionStore.saveAssignment(this, assignmentId)
        startAsForeground()
        reporter?.stop()
        reporter = LocationReporter(this) { lat, lon, speed ->
            postTrack(token, assignmentId, lat, lon, speed)
        }.also { it.start() }
        return START_STICKY
    }

    override fun onDestroy() {
        tearDown()
        super.onDestroy()
    }

    private fun tearDown() {
        reporter?.stop()
        reporter = null
        SessionStore.clearAssignment(this)
        stopForeground(STOP_FOREGROUND_REMOVE)
    }

    private fun startAsForeground() {
        val manager = getSystemService(NotificationManager::class.java)
        manager.createNotificationChannel(
            NotificationChannel(CHANNEL, "Rastreio GPS", NotificationManager.IMPORTANCE_LOW)
        )
        val notification = NotificationCompat.Builder(this, CHANNEL)
            .setContentTitle("Rastreando deslocamento")
            .setContentText("Sua posição está sendo enviada ao cliente.")
            .setSmallIcon(android.R.drawable.ic_menu_mylocation)
            .setOngoing(true)
            .build()
        if (Build.VERSION.SDK_INT >= 29) {
            startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_LOCATION)
        } else {
            startForeground(NOTIFICATION_ID, notification)
        }
    }

    private fun postTrack(token: String, assignmentId: String, lat: Double, lon: Double, speed: Double?) {
        io.execute {
            val json = """{"latitude":$lat,"longitude":$lon,"speedKmh":${speed ?: "null"}}"""
            val body = json.toRequestBody("application/json".toMediaType())
            val request = Request.Builder()
                .url(BuildConfig.API_BASE_URL.trimEnd('/') + "/api/intranet/technician/visits/$assignmentId/track")
                .addHeader("Authorization", "Bearer $token")
                .post(body)
                .build()
            runCatching { http.newCall(request).execute().close() }
        }
    }

    companion object {
        private const val CHANNEL = "gps_tracking"
        private const val NOTIFICATION_ID = 42
        const val EXTRA_ASSIGNMENT = "assignmentId"
        const val ACTION_STOP = "com.manutencao.tecnico.STOP_TRACKING"

        fun start(context: Context, assignmentId: String) {
            SessionStore.saveAssignment(context, assignmentId)
            val intent = Intent(context, TrackingService::class.java)
                .putExtra(EXTRA_ASSIGNMENT, assignmentId)
            if (Build.VERSION.SDK_INT >= 26) context.startForegroundService(intent)
            else context.startService(intent)
        }

        fun stop(context: Context) {
            context.stopService(Intent(context, TrackingService::class.java))
        }
    }
}
