package com.naverlabs.android.examples.vlsdkarceyeapp

import android.annotation.SuppressLint
import android.content.Context
import android.util.AttributeSet
import android.view.MotionEvent
import android.view.SurfaceHolder
import android.view.SurfaceView

class MySurfaceView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
): SurfaceView(context, attrs, defStyleAttr), SurfaceHolder.Callback {

    var listener: MySurfaceViewListener? = null

    init {
        holder.addCallback(this)
    }

    override fun surfaceCreated(holder: SurfaceHolder) {
        listener?.onSurfaceCreated(holder)
    }

    override fun surfaceChanged(holder: SurfaceHolder, format: Int, width: Int, height: Int) {
        listener?.onSurfaceChanged(holder, format, width, height)
    }

    override fun surfaceDestroyed(holder: SurfaceHolder) {
        listener?.onSurfaceDestroyed(holder)
    }

    override fun onTouchEvent(event: MotionEvent): Boolean {
        when (event.action) {
            MotionEvent.ACTION_DOWN -> {
                // Must return true on ACTION_DOWN to receive subsequent events (ACTION_UP)
                return true
            }
            MotionEvent.ACTION_UP -> {
                val normalizedX = event.x / width
                val normalizedY = event.y / height
                listener?.onTap(normalizedX, normalizedY)
                return true
            }
        }
        return super.onTouchEvent(event)
    }
}
