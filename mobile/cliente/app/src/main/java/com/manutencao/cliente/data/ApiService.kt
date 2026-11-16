package com.manutencao.cliente.data

import com.manutencao.cliente.BuildConfig
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

data class LoginRequest(val email: String, val password: String)
data class LoginResponse(val token: String, val fullName: String? = null)
data class CreateServiceRequest(
    val title: String,
    val description: String,
    val category: String,
    val priority: Int,
    val addressLine: String,
    val city: String,
    val state: String,
    val zipCode: String,
    val latitude: Double,
    val longitude: Double
)
data class ServiceRequestSummary(
    val id: String,
    val protocol: String,
    val title: String,
    val description: String,
    val status: String,
    val priority: String? = null,
    val addressLine: String? = null
)
data class ClientRequestDetail(
    val id: String,
    val assignmentId: String?,
    val protocol: String,
    val title: String,
    val description: String,
    val status: String,
    val priority: String?,
    val addressLine: String,
    val city: String?,
    val latitude: Double,
    val longitude: Double,
    val createdAt: String?,
    val technicianName: String?,
    val technicianLatitude: Double?,
    val technicianLongitude: Double?,
    val technicianLocationAt: String?,
    val proofPhotoUrl: String?,
    val completionNotes: String?,
    val feedbackRating: Int?,
    val feedbackComment: String?
)
data class TechnicianTracking(
    val requestId: String,
    val assignmentId: String?,
    val status: String,
    val trackingAvailable: Boolean,
    val technicianName: String?,
    val destinationLatitude: Double,
    val destinationLongitude: Double,
    val technicianLatitude: Double?,
    val technicianLongitude: Double?,
    val technicianLocationAt: String?,
    val distanceKm: Double? = null,
    val etaMinutes: Double? = null
)
data class SubmitFeedback(val rating: Int, val comment: String?)

interface ApiService {
    @POST("api/auth/login")
    suspend fun login(@Body body: LoginRequest): LoginResponse

    @POST("api/client/requests")
    suspend fun create(@Body body: CreateServiceRequest)

    @GET("api/client/requests")
    suspend fun myRequests(): List<ServiceRequestSummary>

    @GET("api/client/requests/{id}")
    suspend fun detail(@Path("id") id: String): ClientRequestDetail

    @GET("api/client/requests/{id}/tracking")
    suspend fun tracking(@Path("id") id: String): TechnicianTracking

    @POST("api/client/requests/{id}/feedback")
    suspend fun feedback(@Path("id") id: String, @Body body: SubmitFeedback)

    companion object {
        fun create(tokenProvider: () -> String?): ApiService {
            val auth = Interceptor { chain ->
                val token = tokenProvider()
                val req = if (!token.isNullOrBlank()) {
                    chain.request().newBuilder().addHeader("Authorization", "Bearer $token").build()
                } else chain.request()
                chain.proceed(req)
            }
            return Retrofit.Builder()
                .baseUrl(BuildConfig.API_BASE_URL)
                .client(OkHttpClient.Builder().addInterceptor(auth).build())
                .addConverterFactory(GsonConverterFactory.create())
                .build()
                .create(ApiService::class.java)
        }
    }
}
