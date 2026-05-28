package com.naverlabs.android.examples.vlsdkarceyeapp

import android.view.SurfaceHolder

interface MySurfaceViewListener {
    fun onSurfaceCreated(holder: SurfaceHolder)
    fun onSurfaceChanged(holder: SurfaceHolder, format: Int, width: Int, height: Int)
    fun onSurfaceDestroyed(holder: SurfaceHolder)
    fun onTap(normalizedX: Float, normalizedY: Float) {}
}