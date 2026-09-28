package io.recam.recam

import android.app.ActivityManager
import android.app.NotificationManager
import android.content.Intent
import android.content.IntentFilter
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.os.BatteryManager
import android.os.Build
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, CHANNEL)
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "batteryTemperature" -> result.success(batteryTemperature())
                    "manufacturer" -> result.success(Build.MANUFACTURER)
                    // Android 9+: the person restricted the app's background use in its settings.
                    "isBackgroundRestricted" -> result.success(
                        getSystemService(ActivityManager::class.java).isBackgroundRestricted
                    )
                    // The camera's ongoing notification is what keeps it alive in the background.
                    "areNotificationsEnabled" -> result.success(
                        getSystemService(NotificationManager::class.java).areNotificationsEnabled()
                    )
                    "hasNetwork" -> result.success(hasNetwork())
                    else -> result.notImplemented()
                }
            }
    }

    // The battery broadcast is sticky: reading it with a null receiver registers nothing.
    private fun batteryTemperature(): Double? {
        val status = registerReceiver(null, IntentFilter(Intent.ACTION_BATTERY_CHANGED)) ?: return null
        val tenths = status.getIntExtra(BatteryManager.EXTRA_TEMPERATURE, Int.MIN_VALUE)
        return if (tenths == Int.MIN_VALUE) null else tenths / 10.0
    }

    // Any network that says it reaches beyond the phone; whether the server answers is the
    // connection's own business.
    private fun hasNetwork(): Boolean {
        val connectivity = getSystemService(ConnectivityManager::class.java)
        val capabilities = connectivity.getNetworkCapabilities(connectivity.activeNetwork) ?: return false
        return capabilities.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
    }

    private companion object {
        const val CHANNEL = "io.recam.app/device"
    }
}
