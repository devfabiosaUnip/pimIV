package com.manutencao.tecnico.ui

import android.annotation.SuppressLint
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.viewinterop.AndroidView

@SuppressLint("SetJavaScriptEnabled")
@Composable
fun LeafletMap(lat: Double, lng: Double, modifier: Modifier = Modifier) {
    val html = """
        <!doctype html><html><head>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
        <style>html,body,#map{height:100%;margin:0;background:#0b1220}</style>
        </head><body>
        <div id="map"></div>
        <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
        <script>
          const dest = [$lat, $lng];
          const map = L.map('map', { zoomControl: false }).setView(dest, 15);
          L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {attribution:'© OSM'}).addTo(map);
          L.circleMarker(dest, {radius:10,color:'#38bdf8',fillColor:'#38bdf8',fillOpacity:0.9}).addTo(map).bindPopup('Cliente');
        </script>
        </body></html>
    """.trimIndent()
    AndroidView(modifier = modifier, factory = { context ->
        WebView(context).apply {
            settings.javaScriptEnabled = true
            settings.domStorageEnabled = true
            webViewClient = WebViewClient()
            loadDataWithBaseURL("https://unpkg.com", html, "text/html", "utf-8", null)
        }
    })
}
