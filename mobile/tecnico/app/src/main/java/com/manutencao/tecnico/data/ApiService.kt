package com.manutencao.tecnico.data

import com.manutencao.tecnico.BuildConfig
import okhttp3.Interceptor
import okhttp3.MultipartBody
import okhttp3.OkHttpClient
import okhttp3.RequestBody
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.Part
import retrofit2.http.Path

interface ApiService {

    @POST("api/auth/login")
    suspend fun login(@Body body: LoginRequest): LoginResponse

    @GET("api/intranet/technician/visits")
    suspend fun visits(): List<TechnicianVisitDto>

    @POST("api/intranet/technician/location")
    suspend fun updateLocation(@Body body: LocationUpdate)

    @POST("api/intranet/technician/visits/{id}/track")
    suspend fun track(
        @Path("id") assignmentId: String,
        @Body body: LocationUpdate
    )

    @POST("api/intranet/technician/visits/{id}/status")
    suspend fun updateStatus(
        @Path("id") assignmentId: String,
        @Body body: StatusUpdate
    ): Response<Unit>

    @Multipart
    @POST("api/intranet/technician/visits/{id}/complete")
    suspend fun complete(
        @Path("id") assignmentId: String,
        @Part photo: MultipartBody.Part,
        @Part("notes") notes: RequestBody
    ): Response<Unit>

    companion object {
        fun create(tokenProvider: () -> String?): ApiService {
            val auth = Interceptor { chain ->
                val token = tokenProvider()

                val request = if (!token.isNullOrBlank()) {
                    chain.request()
                        .newBuilder()
                        .addHeader("Authorization", "Bearer $token")
                        .build()
                } else {
                    chain.request()
                }

                chain.proceed(request)
            }

            val client = OkHttpClient.Builder()
                .addInterceptor(auth)
                .addInterceptor(
                    HttpLoggingInterceptor().apply {
                        level = HttpLoggingInterceptor.Level.BASIC
                    }
                )
                .build()

            return Retrofit.Builder()
                .baseUrl(BuildConfig.API_BASE_URL)
                .client(client)
                .addConverterFactory(GsonConverterFactory.create())
                .build()
                .create(ApiService::class.java)
        }
    }
}