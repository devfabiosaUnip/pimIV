package com.manutencao.tecnico.vpn

import android.content.Context
import android.content.Intent
import android.net.Uri
import java.io.File

/**
 * Integração com WireGuard oficial (intranet).
 * O perfil .conf é gerado pelo servidor Docker em vpn/wireguard/config/.
 */
object VpnHelper {
    fun openWireGuardImport(context: Context, confAssetName: String = "tecnico1.conf") {
        val out = File(context.cacheDir, confAssetName)
        context.assets.open(confAssetName).use { input ->
            out.outputStream().use { input.copyTo(it) }
        }
        val intent = Intent(Intent.ACTION_VIEW).apply {
            setDataAndType(Uri.fromFile(out), "application/x-wireguard-profile")
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        context.startActivity(intent)
    }
}
