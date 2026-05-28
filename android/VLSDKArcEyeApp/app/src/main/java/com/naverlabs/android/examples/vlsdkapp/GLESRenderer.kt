package com.naverlabs.android.examples.vlsdkarceyeapp

class GLESRenderer {
    companion object {
        private val TAG = GLESRenderer::class.java.simpleName
        private const val HIT_CUBE_SCALE = 0.05f
    }

    private val cameraPreview: CameraPreview
    private val coloredCube: ColoredCube
    private val hitCubes: MutableList<ColoredCube> = mutableListOf()
    private var cubeYAngle: Float

    init {
        cameraPreview = CameraPreview()

        coloredCube = ColoredCube()
        coloredCube.setPosition(43.0f, 1.0f, -16.0f)
        coloredCube.setScale(0.9f, 0.9f, 0.9f)
        cubeYAngle = 0.0f
    }

    fun drawCameraPreview(textureTransform: FloatArray, textureId: Int) {
        cameraPreview.draw(textureTransform, textureId)
    }

    fun drawCube(viewMatrix: DoubleArray, projectionMatrix: FloatArray) {
        var newAngle = cubeYAngle + 2.0f
        if(newAngle >= 360.0f) {
            newAngle -= 360.0f
        }
        coloredCube.setRotation(0.0f, newAngle, 0.0f)
        cubeYAngle = newAngle

        coloredCube.draw(viewMatrix, projectionMatrix)

        // Draw all hit cubes
        for (cube in hitCubes) {
            cube.setRotation(0.0f, newAngle, 0.0f)
            cube.draw(viewMatrix, projectionMatrix)
        }
    }

    fun addCube(x: Float, y: Float, z: Float) {
        val cube = ColoredCube()
        cube.setPosition(x, y, z)
        cube.setScale(HIT_CUBE_SCALE, HIT_CUBE_SCALE, HIT_CUBE_SCALE)
        hitCubes.add(cube)
    }

    /**
     * Clears all hit cubes.
     */
    fun clearHitCubes() {
        hitCubes.clear()
    }
}