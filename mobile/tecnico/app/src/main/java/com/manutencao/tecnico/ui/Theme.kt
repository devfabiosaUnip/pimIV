package com.manutencao.tecnico.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val TecnicoColors = darkColorScheme(
    primary = Color(0xFF93C5FD),
    onPrimary = Color(0xFF0B1220),
    secondary = Color(0xFFFBBF24),
    background = Color(0xFF0B1220),
    surface = Color(0xFF111827),
    onBackground = Color(0xFFE2E8F0),
    onSurface = Color(0xFFE2E8F0),
    error = Color(0xFFF87171)
)

@Composable
fun TecnicoTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = TecnicoColors, content = content)
}

fun statusLabel(status: String) = when (status) {
    "Atribuida" -> "Atribuída"
    "EmDeslocamento" -> "Em deslocamento"
    "EmAtendimento" -> "Em atendimento"
    "Concluida" -> "Concluída"
    else -> status
}
