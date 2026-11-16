package com.manutencao.tecnico.data

import android.content.Context

object SessionStore {
    private const val PREFS = "manutencao_tecnico"

    fun saveToken(context: Context, token: String) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putString("token", token).apply()
    }

    fun token(context: Context): String? =
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getString("token", null)

    fun saveAssignment(context: Context, assignmentId: String) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putString("assignment", assignmentId).apply()
    }

    fun assignmentId(context: Context): String? =
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getString("assignment", null)

    fun clearAssignment(context: Context) {
        context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().remove("assignment").apply()
    }
}
