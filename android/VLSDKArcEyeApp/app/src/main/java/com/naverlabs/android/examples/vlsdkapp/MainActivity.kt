package com.naverlabs.android.examples.vlsdkarceyeapp

import android.content.pm.PackageManager
import android.graphics.Color
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.util.Log
import android.util.TypedValue
import android.view.SurfaceHolder
import android.view.View
import android.widget.Button
import android.widget.FrameLayout
import android.widget.LinearLayout
import android.widget.SeekBar
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.constraintlayout.widget.ConstraintLayout
import androidx.constraintlayout.widget.ConstraintSet
import androidx.core.content.ContextCompat
import com.naverlabs.android.vlsdk.OnUpdateDatasetInfoListener
import com.naverlabs.android.vlsdk.OnUpdateFrameListener
import com.naverlabs.android.vlsdk.OnUpdateStatusListener
import com.naverlabs.android.vlsdk.VLSDKBuilder
import com.naverlabs.android.vlsdk.VLSDKConfig
import com.naverlabs.android.vlsdk.VLSDKDecoderConfig
import com.naverlabs.android.vlsdk.VLSDKFrame
import com.naverlabs.android.vlsdk.VLSDKLogLevel
import org.ejml.simple.SimpleMatrix
import com.naverlabs.android.vlsdk.VLSDKService
import com.naverlabs.android.vlsdk.VLSDKSession
import com.naverlabs.android.vlsdk.VLSDKStatus
import java.util.Locale


class MainActivity : AppCompatActivity(), MySurfaceViewListener {
    companion object {
        private val TAG = MainActivity::class.java.name
    }

    private val PERMISSIONS: Array<String> =
        arrayOf(
            android.Manifest.permission.CAMERA
        )

    private val requestPermissionsLauncher =
        registerForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { permissions ->
            val allGranted = permissions.values.all { it }

            if (allGranted) {
                setupVLSDKSession()
                setupRootLayout()
            } else {
                Toast.makeText(this, "Cannot run application without permissions", Toast.LENGTH_SHORT).show()
                finish()
            }
        }

    private var session: VLSDKSession = VLSDKSession.shared()
    private var surfaceView: MySurfaceView? = null
    private val renderer: GLESRenderer = GLESRenderer()
    private var dragging: Boolean = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        if (hasAllPermissions()) {
            setupVLSDKSession()
            setupRootLayout()
        } else {
            requestPermissionsLauncher.launch(PERMISSIONS)
        }
    }

    private fun hasAllPermissions(): Boolean {
        return PERMISSIONS.all { permission ->
            ContextCompat.checkSelfPermission(this, permission) == PackageManager.PERMISSION_GRANTED
        }
    }

    override fun onResume() {
        super.onResume()
        // session.resume()
    }

    override fun onPause() {
        super.onPause()
        session.reset()
        session.pause()
    }

    override fun onDestroy() {
        super.onDestroy()
        session.destroy()
    }

    /* MySurfaceViewListener */
    override fun onSurfaceCreated(holder: SurfaceHolder) {
        session.createNativeLayer(holder.surface)
    }

    override fun onSurfaceChanged(holder: SurfaceHolder, format: Int, width: Int, height: Int) {
        session.changeViewport(width, height)
    }

    override fun onSurfaceDestroyed(holder: SurfaceHolder) {
        session.destroyNativeLayer()
    }

    override fun onTap(normalizedX: Float, normalizedY: Float) {
        session.raycast(normalizedX, normalizedY) { hit ->
            hit?.let {
                val pos = it.position
                renderer.addCube(pos[0].toFloat(), pos[1].toFloat(), pos[2].toFloat())
            }
        }
    }

    private fun setupVLSDKSession() {
        val services = arrayOf(
            VLSDKService("VLSDK_ARCEYE_LOCATION_NAME",
                "VLSDK_ARCEYE_VL_API_URL",
                "VLSDK_ARCEYE_VL_API_KEY"
            )
        )

        val config = VLSDKConfig(services)

        config.logLevel = VLSDKLogLevel.WARNING
        config.decoderConfig = if (isEmulator()) VLSDKDecoderConfig() else null
        config.useRaycast = true
        config.onUpdateStatus = null;

        config.onUpdateStatus = OnUpdateStatusListener { status ->
            runOnUiThread {
                when(status) {
                    VLSDKStatus.INITIAL -> { statusTextView.setTextColor(Color.BLACK) }
                    VLSDKStatus.NOT_RECOGNIZED -> { statusTextView.setTextColor(Color.RED) }
                    VLSDKStatus.VL_PASS -> { statusTextView.setTextColor(Color.GREEN) }
                    VLSDKStatus.VL_FAIL -> { statusTextView.setTextColor(Color.RED) }
                    else -> { statusTextView.setTextColor(Color.BLACK) }
                }

                val statusText = "$status"
                statusTextView.text = statusText
            }

            if(status == VLSDKStatus.INITIAL){
                val datasetInfoText = "N/A"
                runOnUiThread {
                    datasetInfoTextView.text = datasetInfoText
                }
            }
        }

        config.onUpdateFrame = OnUpdateFrameListener { frame ->
            renderer.drawCameraPreview(frame.textureTransform, frame.gpuTexture)
            renderer.drawCube(frame.viewMatrix, frame.projMatrix)

            val (camX, camY, camZ) = getCameraPosition(frame.viewMatrix)

            val txt = """
                timestamp: ${frame.timestamp}
                bearing: ${frame.bearing}
                position: x=%.3f y=%.3f z=%.3f
            """.trimIndent().format(camX, camY, camZ)

            runOnUiThread {
                frameInfoTextView.text = txt
            }
        }

        config.onUpdateDatasetInfo = OnUpdateDatasetInfoListener { datasetInfo ->
            runOnUiThread {
                datasetInfoTextView.text = datasetInfo
            }
        }

        session.setup(this, config)

        surfaceView = MySurfaceView(this)
        surfaceView?.listener = this

        cameraPreviewContainer.addView(
            surfaceView,
            FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
            )
        )

        session.decoder?.let {

        }?: run {
            btnResume.setOnClickListener {
                session.resume()
                session.reset()
            }

            btnPause.setOnClickListener {
                session.pause()
                session.reset()
            }
        }
    }

    private fun dpToPx(dp: Int): Int {
        return TypedValue.applyDimension(
            TypedValue.COMPLEX_UNIT_DIP,
            dp.toFloat(),
            resources.displayMetrics
        ).toInt()
    }

    private fun setupRootLayout() {
        rootLayout.apply {
            addView(cameraPreviewContainer)
            addView(infoContainer)
            addView(buttonContainer)
        }

        ConstraintSet().apply {
            clone(rootLayout)

            connect(cameraPreviewContainer.id, ConstraintSet.TOP, rootLayout.id, ConstraintSet.TOP)
            connect(cameraPreviewContainer.id, ConstraintSet.START, rootLayout.id, ConstraintSet.START)
            connect(cameraPreviewContainer.id, ConstraintSet.END, rootLayout.id, ConstraintSet.END)

            setMargin(cameraPreviewContainer.id, ConstraintSet.START, dpToPx(48))
            setMargin(cameraPreviewContainer.id, ConstraintSet.END, dpToPx(48))
            setMargin(cameraPreviewContainer.id, ConstraintSet.TOP, dpToPx(12))

            constrainWidth(cameraPreviewContainer.id, 0)
            constrainHeight(cameraPreviewContainer.id, 0)

            setDimensionRatio(cameraPreviewContainer.id, "9:16")

            connect(infoContainer.id, ConstraintSet.TOP, cameraPreviewContainer.id, ConstraintSet.BOTTOM)
            connect(infoContainer.id, ConstraintSet.START, rootLayout.id, ConstraintSet.START)
            connect(infoContainer.id, ConstraintSet.END, rootLayout.id, ConstraintSet.END)
            constrainWidth(infoContainer.id, ConstraintSet.MATCH_CONSTRAINT)
            constrainHeight(infoContainer.id, ConstraintSet.WRAP_CONTENT)

            connect(buttonContainer.id, ConstraintSet.TOP, infoContainer.id, ConstraintSet.BOTTOM)
            connect(buttonContainer.id, ConstraintSet.START, rootLayout.id, ConstraintSet.START)
            connect(buttonContainer.id, ConstraintSet.END, rootLayout.id, ConstraintSet.END)
            connect(buttonContainer.id, ConstraintSet.BOTTOM, rootLayout.id, ConstraintSet.BOTTOM)
            constrainWidth(buttonContainer.id, ConstraintSet.MATCH_CONSTRAINT)
            constrainHeight(buttonContainer.id, ConstraintSet.WRAP_CONTENT)

            applyTo(rootLayout)
        }

        infoContainer.apply {
            addView(statusTextView)
            addView(datasetInfoTextView)
            addView(frameInfoTextView)
        }

        session.decoder?.let {
            buttonContainer.apply {
                addView(seekBar)
                addView(btnPlayPause)
                addView(btnOpenFile)
            }

            ConstraintSet().apply {
                clone(buttonContainer)

                constrainWidth(seekBar.id, 0)
                constrainHeight(seekBar.id, ConstraintSet.WRAP_CONTENT)

                constrainWidth(btnPlayPause.id, 0)
                constrainHeight(btnPlayPause.id, ConstraintSet.WRAP_CONTENT)

                constrainWidth(btnOpenFile.id, 0)
                constrainHeight(btnOpenFile.id, ConstraintSet.WRAP_CONTENT)

                val verticalMargin = dpToPx(16)
                connect(seekBar.id, ConstraintSet.TOP, buttonContainer.id, ConstraintSet.TOP, verticalMargin)
                connect(seekBar.id, ConstraintSet.BOTTOM, buttonContainer.id, ConstraintSet.BOTTOM, verticalMargin)

                connect(btnPlayPause.id, ConstraintSet.TOP, buttonContainer.id, ConstraintSet.TOP, verticalMargin)
                connect(btnPlayPause.id, ConstraintSet.BOTTOM, buttonContainer.id, ConstraintSet.BOTTOM, verticalMargin)

                connect(btnOpenFile.id, ConstraintSet.TOP, buttonContainer.id, ConstraintSet.TOP, verticalMargin)
                connect(btnOpenFile.id, ConstraintSet.BOTTOM, buttonContainer.id, ConstraintSet.BOTTOM, verticalMargin)

                createHorizontalChain(
                    buttonContainer.id, ConstraintSet.LEFT,
                    buttonContainer.id, ConstraintSet.RIGHT,
                    intArrayOf(seekBar.id, btnPlayPause.id, btnOpenFile.id),
                    floatArrayOf(8f, 1f, 1f),
                    ConstraintSet.CHAIN_SPREAD
                )

                val outerMargin = dpToPx(16)
                val betweenMargin = dpToPx(8)

                setMargin(seekBar.id, ConstraintSet.START, outerMargin)
                setMargin(btnPlayPause.id, ConstraintSet.START, betweenMargin)
                setMargin(btnOpenFile.id, ConstraintSet.START, betweenMargin)
                setMargin(btnOpenFile.id, ConstraintSet.END, outerMargin)

                seekBar.progressDrawable.setColorFilter(
                    Color.RED, android.graphics.PorterDuff.Mode.SRC_IN)

                applyTo(buttonContainer)
            }
        }?: run {
            buttonContainer.addView(btnResume)
            buttonContainer.addView(btnPause)

            ConstraintSet().apply {
                clone(buttonContainer)

                connect(btnResume.id, ConstraintSet.START, buttonContainer.id, ConstraintSet.START, dpToPx(16))
                connect(btnResume.id, ConstraintSet.TOP, buttonContainer.id, ConstraintSet.TOP, dpToPx(8))
                connect(btnResume.id, ConstraintSet.BOTTOM, buttonContainer.id, ConstraintSet.BOTTOM, dpToPx(8))

                connect(btnPause.id, ConstraintSet.END, buttonContainer.id, ConstraintSet.END, dpToPx(16))
                connect(btnPause.id, ConstraintSet.TOP, buttonContainer.id, ConstraintSet.TOP, dpToPx(8))
                connect(btnPause.id, ConstraintSet.BOTTOM, buttonContainer.id, ConstraintSet.BOTTOM, dpToPx(8))

                applyTo(buttonContainer)
            }
        }

        setContentView(rootLayout)
    }

    private val rootLayout by lazy {
        ConstraintLayout(this).apply {
            id = View.generateViewId()
            layoutParams = ConstraintLayout.LayoutParams(
                ConstraintLayout.LayoutParams.MATCH_PARENT,
                ConstraintLayout.LayoutParams.MATCH_PARENT
            )
            setBackgroundColor(Color.WHITE)
        }
    }

    private val cameraPreviewContainer by lazy {
        FrameLayout(this).apply {
            id = View.generateViewId()
            setBackgroundColor(Color.LTGRAY)
        }
    }

    private val infoContainer by lazy {
        LinearLayout(this).apply {
            id = View.generateViewId()
            orientation = LinearLayout.VERTICAL
            setBackgroundColor(Color.LTGRAY)
            setPadding(20, 20, 20, 20)
        }
    }

    private val buttonContainer by lazy {
        ConstraintLayout(this).apply {
            id = View.generateViewId()
            setBackgroundColor(Color.WHITE)
        }
    }

    private val statusTextView by lazy {
        TextView(this).apply {
            text = "N/A"
            textSize = 10f
            setTextColor(Color.BLACK)
        }
    }

    private val datasetInfoTextView by lazy {
        TextView(this).apply {
            text = "N/A"
            textSize = 10f
            setTextColor(Color.BLACK)
        }
    }

    private val frameInfoTextView by lazy {
        TextView(this).apply {
            text = "N/A"
            textSize = 10f
            setTextColor(Color.BLACK)
        }
    }

    private val btnResume by lazy {
        Button(this).apply {
            id = View.generateViewId()
            text = "Resume"
            setBackgroundColor(Color.RED)
            setTextColor(Color.WHITE)
        }
    }

    private val btnPause by lazy {
        Button(this).apply {
            id = View.generateViewId()
            text = "Pause"
            setBackgroundColor(Color.BLUE)
            setTextColor(Color.WHITE)
        }
    }

    private val seekBar by lazy {
        SeekBar(this).apply {
            id = View.generateViewId()
            max = 100
            setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
                override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {

                }

                override fun onStartTrackingTouch(seekBar: SeekBar?) {
                    seekBar?.progress?.let { p ->
                        dragging = true
                    }
                }

                override fun onStopTrackingTouch(seekBar: SeekBar?) {
                    seekBar?.progress?.let { p ->
                        session.reset()
                        session.resume()
                        session.decoder?.seek(p.toFloat() / 100f)
                        dragging = false
                    }
                }
            })
        }
    }

    private val btnPlayPause by lazy {
        Button(this).apply {
            id = View.generateViewId()
            text = ""
            setCompoundDrawablesWithIntrinsicBounds(android.R.drawable.ic_media_play, 0, 0, 0)

            val typedValue = TypedValue()
            theme.resolveAttribute(android.R.attr.selectableItemBackgroundBorderless, typedValue, true)
            setBackgroundResource(typedValue.resourceId)
            val pad = dpToPx(8)
            setPadding(pad, pad, pad, pad)
        }
    }

    private val btnOpenFile by lazy {
        Button(this).apply {
            id = View.generateViewId()
            text = ""
            setCompoundDrawablesWithIntrinsicBounds(android.R.drawable.ic_menu_gallery, 0, 0, 0)

            val typedValue = TypedValue()
            theme.resolveAttribute(android.R.attr.selectableItemBackgroundBorderless, typedValue, true)
            setBackgroundResource(typedValue.resourceId)

            val pad = dpToPx(8)
            setPadding(pad, pad, pad, pad)

            setOnClickListener {
                openFileLauncher.launch(arrayOf("video/mp4"))
            }
        }
    }

    private val openFileLauncher = registerForActivityResult(ActivityResultContracts.OpenDocument()) { uri: Uri? ->
        uri?.let {
            Log.d(TAG, "Selected URI: $it")
            session.decoder?.importDataset(it)

            session.decoder?.setOnProgress {progress ->
                runOnUiThread {
                    if(!dragging) {
                        seekBar.progress = (progress * 100).toInt()
                    }
                }
            }

            session.decoder?.setOnPlaying {isPlaying ->
                runOnUiThread {
                    if (isPlaying) {
                        btnPlayPause.setCompoundDrawablesWithIntrinsicBounds(
                            android.R.drawable.ic_media_pause, 0, 0, 0
                        )

                        btnPlayPause.setOnClickListener {
                            session.pause()
                        }
                    } else {
                        btnPlayPause.setCompoundDrawablesWithIntrinsicBounds(
                            android.R.drawable.ic_media_play, 0, 0, 0
                        )

                        btnPlayPause.setOnClickListener {
                            session.resume()
                        }
                    }
                }
            }

            session.resume()
        }
    }

    private fun isEmulator(): Boolean {
        val product = Build.PRODUCT
        val device = Build.DEVICE
        val model = Build.MODEL
        val brand = Build.BRAND
        val manufacturer = Build.MANUFACTURER
        val knownEmulatorIndicators = arrayOf(
            "sdk", "emulator", "generic", "google_sdk", "sdk_google", "sdk_x86", "vbox"
        )
        for (indicator in knownEmulatorIndicators) {
            if (product != null && product.lowercase(Locale.getDefault())
                    .contains(indicator) || device != null && device.lowercase(
                    Locale.getDefault()
                ).contains(indicator) || model != null && model.lowercase(Locale.getDefault())
                    .contains(indicator) || brand != null && brand.lowercase(
                    Locale.getDefault()
                ).contains(indicator) || manufacturer != null && manufacturer.lowercase(
                    Locale.getDefault()
                ).contains(indicator)
            ) {
                return true
            }
        }
        return false
    }

    fun getCameraPosition(viewMatrix: DoubleArray): Triple<Double, Double, Double> {
        val m = SimpleMatrix(4, 4, true, *viewMatrix)
        val inv = m.invert()
        val camX = inv.get(3, 0)
        val camY = inv.get(3, 1)
        val camZ = inv.get(3, 2)
        return Triple(camX, camY, camZ)
    }
}