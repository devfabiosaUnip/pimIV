package com.manutencao.tecnico.data

data class LoginRequest(val email: String, val password: String)
data class LoginResponse(val token: String, val fullName: String, val role: String, val userId: String)

data class TechnicianVisitDto(
    val assignmentId: String,
    val serviceRequestId: String,
    val protocol: String,
    val title: String,
    val description: String,
    val addressLine: String,
    val city: String,
    val latitude: Double,
    val longitude: Double,
    val visitStatus: String,
    val scheduledFor: String?
)

data class LocationUpdate(val latitude: Double, val longitude: Double, val speedKmh: Double? = null)
data class StatusUpdate(val status: String)
