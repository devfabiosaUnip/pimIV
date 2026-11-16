package com.manutencao.tecnico.gps

import android.annotation.SuppressLint
import android.content.Context
import android.location.LocationListener
import android.location.LocationManager
import android.os.Looper

class LocationReporter(
    context: Context,
    private val onLocation: (lat: Double, lon: Double, speedKmh: Double?) -> Unit
) {
    private val manager = context.getSystemService(Context.LOCATION_SERVICE) as LocationManager
    private val listener = LocationListener { loc ->
        val speed = if (loc.hasSpeed()) loc.speed * 3.6 else null
        onLocation(loc.latitude, loc.longitude, speed)
    }

    @SuppressLint("MissingPermission")
    fun start() {
        stop()
        val providers = listOf(LocationManager.GPS_PROVIDER, LocationManager.NETWORK_PROVIDER)
            .filter { runCatching { manager.isProviderEnabled(it) }.getOrDefault(false) }
            .ifEmpty { listOf(LocationManager.GPS_PROVIDER) }

        providers.forEach { provider ->
            runCatching {
                manager.requestLocationUpdates(provider, 4000L, 6f, listener, Looper.getMainLooper())
            }
        }

        val last = runCatching { manager.getLastKnownLocation(LocationManager.GPS_PROVIDER) }.getOrNull()
            ?: runCatching { manager.getLastKnownLocation(LocationManager.NETWORK_PROVIDER) }.getOrNull()
        last?.let { onLocation(it.latitude, it.longitude, if (it.hasSpeed()) it.speed * 3.6 else null) }
    }

    fun stop() {
        runCatching { manager.removeUpdates(listener) }
    }
}
