package io.recam.recam

import android.content.Intent
import android.content.IntentFilter
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

    private companion object {
        const val CHANNEL = "io.recam.app/device"
    }
}
