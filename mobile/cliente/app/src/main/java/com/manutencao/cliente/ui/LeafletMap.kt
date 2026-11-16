package com.manutencao.cliente.ui

import android.annotation.SuppressLint
import android.webkit.WebSettings
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.viewinterop.AndroidView

@SuppressLint("SetJavaScriptEnabled")
@Composable
fun LeafletMap(
    destinationLat: Double,
    destinationLng: Double,
    technicianLat: Double?,
    technicianLng: Double?,
    modifier: Modifier = Modifier
) {
    val html = remember(destinationLat, destinationLng) {
        mapHtml(destinationLat, destinationLng)
    }
    AndroidView(
        modifier = modifier,
        factory = { context ->
            WebView(context).apply {
                settings.javaScriptEnabled = true
                settings.domStorageEnabled = true
                settings.mixedContentMode = WebSettings.MIXED_CONTENT_ALWAYS_ALLOW
                webViewClient = WebViewClient()
                loadDataWithBaseURL("https://unpkg.com", html, "text/html", "utf-8", null)
            }
        },
        update = { view ->
            if (technicianLat != null && technicianLng != null) {
                view.evaluateJavascript("updateTech($technicianLat, $technicianLng);", null)
            }
        }
    )
}

fun mapHtml(destLat: Double, destLng: Double): String {
    return """
        <!doctype html><html><head>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
        <style>html,body,#map{height:100%;margin:0;background:#0b1220}</style>
        </head><body>
        <div id="map"></div>
        <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
        <script>
          const dest = [$destLat, $destLng];
          const map = L.map('map', { zoomControl: false }).setView(dest, 14);
          L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {attribution:'© OSM'}).addTo(map);
          L.circleMarker(dest, {radius:10,color:'#5eead4',fillColor:'#5eead4',fillOpacity:0.95}).addTo(map).bindPopup('Você');
          let tech = null;
          function updateTech(lat, lng) {
            const pos = [lat, lng];
            if (!tech) tech = L.circleMarker(pos, {radius:10,color:'#fbbf24',fillColor:'#fbbf24',fillOpacity:0.95}).addTo(map).bindPopup('Técnico');
            else tech.setLatLng(pos);
            map.fitBounds(L.latLngBounds([dest, pos]).pad(0.28));
          }
        </script>
        </body></html>
    """.trimIndent()
}
