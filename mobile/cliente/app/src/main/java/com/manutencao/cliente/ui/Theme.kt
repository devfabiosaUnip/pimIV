package com.manutencao.cliente.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val ClienteColors = darkColorScheme(
    primary = Color(0xFF5EEAD4),
    onPrimary = Color(0xFF042F2E),
    secondary = Color(0xFF67E8F9),
    background = Color(0xFF0B1220),
    surface = Color(0xFF111827),
    onBackground = Color(0xFFE2E8F0),
    onSurface = Color(0xFFE2E8F0),
    error = Color(0xFFF87171)
)

@Composable
fun ClienteTheme(content: @Composable () -> Unit) {
    MaterialTheme(colorScheme = ClienteColors, content = content)
}

fun statusLabel(status: String) = when (status) {
    "PendenteAprovacao" -> "Aguardando aprovação"
    "AguardandoAtribuicao" -> "Na fila da central"
    "Atribuida" -> "Técnico designado"
    "EmDeslocamento" -> "Técnico a caminho"
    "EmAtendimento" -> "Em atendimento"
    "Concluida" -> "Concluída"
    "Cancelada" -> "Cancelada"
    "Rejeitada" -> "Rejeitada"
    else -> status
}
