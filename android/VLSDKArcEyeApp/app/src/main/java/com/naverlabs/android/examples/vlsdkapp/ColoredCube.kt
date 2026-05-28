package com.naverlabs.android.examples.vlsdkarceyeapp

import android.opengl.GLES30
import android.opengl.Matrix
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.FloatBuffer

class ColoredCube {
    // Shader program object
    private var shaderProgram: Int? = null

    private fun loadShader(type: Int, code: String): Int {
        val shader = GLES30.glCreateShader(type)
        GLES30.glShaderSource(shader, code)
        GLES30.glCompileShader(shader)
        val compiled = IntArray(1)
        GLES30.glGetShaderiv(shader, GLES30.GL_COMPILE_STATUS, compiled, 0)
        if (compiled[0] == 0) {
            val errMsg = GLES30.glGetShaderInfoLog(shader)
            GLES30.glDeleteShader(shader)
            throw RuntimeException("Shader compile error: $errMsg")
        }
        return shader
    }

    private val vertexBuffer: FloatBuffer = ByteBuffer
        .allocateDirect(cubePositions.size * 4)
        .order(ByteOrder.nativeOrder())
        .asFloatBuffer().apply {
            put(cubePositions)
            position(0)
        }

    private val colorBuffer: FloatBuffer = ByteBuffer
        .allocateDirect(cubeColors.size * 4)
        .order(ByteOrder.nativeOrder())
        .asFloatBuffer().apply {
            put(cubeColors)
            position(0)
        }


    private val mvpMatrix = FloatArray(16)
    private var position = floatArrayOf(0.0f, 0.0f, 0.0f)
    private var rotation = floatArrayOf(0.0f, 0.0f, 0.0f)
    private var scale = floatArrayOf(1.0f, 1.0f, 1.0f)
    private val modelMatrix = FloatArray(16)

    init {
        Matrix.setIdentityM(mvpMatrix, 0)
        Matrix.setIdentityM(modelMatrix, 0)
    }

    fun setPosition(x: Float, y: Float, z: Float) {
        position[0] = x
        position[1] = y
        position[2] = z

        updateModelMatrix()
    }

    fun setRotation(x: Float, y: Float, z: Float) {
        rotation[0] = x
        rotation[1] = y
        rotation[2] = z

        updateModelMatrix()
    }

    fun setScale(x: Float, y: Float, z: Float) {
        scale[0] = x
        scale[1] = y
        scale[2] = z

        updateModelMatrix()
    }

    private fun updateModelMatrix() {
        Matrix.setIdentityM(modelMatrix, 0)
        Matrix.translateM(modelMatrix, 0, position[0], position[1], position[2])
        Matrix.rotateM(modelMatrix, 0, rotation[0], 1f, 0f, 0f)
        Matrix.rotateM(modelMatrix, 0, rotation[1], 0f, 1f, 0f)
        Matrix.rotateM(modelMatrix, 0, rotation[2], 0f, 0f, 1f)
        Matrix.scaleM(modelMatrix, 0, scale[0], scale[1], scale[2])
    }

    fun draw(viewMatrixDouble: DoubleArray, projectionMatrix: FloatArray) {
        if(shaderProgram == null) {
            // try compile shaders if program ID is not defined yet
            val vertexShader = loadShader(GLES30.GL_VERTEX_SHADER, vertexShaderCode)
            val fragmentShader = loadShader(GLES30.GL_FRAGMENT_SHADER, fragmentShaderCode)

            val id = GLES30.glCreateProgram()
            shaderProgram = id

            GLES30.glAttachShader(id, vertexShader)
            GLES30.glAttachShader(id, fragmentShader)
            GLES30.glLinkProgram(id)

            val linkStatus = IntArray(1)
            GLES30.glGetProgramiv(id, GLES30.GL_LINK_STATUS, linkStatus, 0)
            if (linkStatus[0] == 0) {
                val errMsg = GLES30.glGetProgramInfoLog(id)
                GLES30.glDeleteProgram(id)
                throw RuntimeException("Program link error: $errMsg")
            }
        }

        // DoubleArray -> FloatArray conversion
        val viewMatrix = FloatArray(16)
        for (i in 0..15) {
            viewMatrix[i] = viewMatrixDouble[i].toFloat()
        }

        // Depth Test ON / BackFace Culling ON
        GLES30.glEnable(GLES30.GL_DEPTH_TEST)
        GLES30.glDepthFunc(GLES30.GL_LEQUAL)

        GLES30.glEnable(GLES30.GL_CULL_FACE)
        GLES30.glCullFace(GLES30.GL_BACK)

        // MVP = Projection * View * Model
        Matrix.multiplyMM(mvpMatrix, 0, viewMatrix, 0, modelMatrix, 0)
        Matrix.multiplyMM(mvpMatrix, 0, projectionMatrix, 0, mvpMatrix, 0)

        // Use shaders
        GLES30.glUseProgram(shaderProgram!!)

        // Setup variables to shaders
        val uMVP = GLES30.glGetUniformLocation(shaderProgram!!, "uMVPMatrix")
        GLES30.glUniformMatrix4fv(uMVP, 1, false, mvpMatrix, 0)

        GLES30.glEnableVertexAttribArray(0)
        GLES30.glVertexAttribPointer(
            0,
            3,
            GLES30.GL_FLOAT,
            false,
            0,
            vertexBuffer
        )

        GLES30.glEnableVertexAttribArray(1)
        GLES30.glVertexAttribPointer(
            1,
            4,
            GLES30.GL_FLOAT,
            false,
            0,
            colorBuffer
        )

        // Draw call
        GLES30.glDrawArrays(GLES30.GL_TRIANGLES, 0, 36)

        // Clean variables
        GLES30.glDisableVertexAttribArray(0)
        GLES30.glDisableVertexAttribArray(1)

        // Depth Test OFF / BackFace Culling OFF
        GLES30.glDisable(GLES30.GL_CULL_FACE)
        GLES30.glDisable(GLES30.GL_DEPTH_TEST)
    }

    companion object {
        private val TAG = ColoredCube::class.java.name
        private val vertexShaderCode = """
            #version 300 es
            layout(location = 0) in vec4 aPosition;
            layout(location = 1) in vec4 aColor;
            uniform mat4 uMVPMatrix;
            out vec4 vColor;
    
            void main() {
                vColor = aColor;
                gl_Position = uMVPMatrix * aPosition;
            }
        """.trimIndent()

        private val fragmentShaderCode = """
            #version 300 es
            precision mediump float;
            in vec4 vColor;
            out vec4 outColor;
    
            void main() {
                outColor = vColor;
            }
        """.trimIndent()

        private val cubePositions = floatArrayOf(
            // Front
            -0.5f, -0.5f,  0.5f,
            0.5f, -0.5f,  0.5f,
            0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,
            0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,

            // Back
            -0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
            0.5f,  0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            0.5f,  0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,

            // Left
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f, -0.5f,

            // Right
            0.5f, -0.5f, -0.5f,
            0.5f,  0.5f, -0.5f,
            0.5f,  0.5f,  0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f,  0.5f,  0.5f,
            0.5f, -0.5f,  0.5f,

            // Top
            -0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f,  0.5f,
            0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f, -0.5f,
            0.5f,  0.5f,  0.5f,
            0.5f,  0.5f, -0.5f,

            // Bottom
            -0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f,
            0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f
        )

        private val cubeColors = floatArrayOf(
            // Front -> Red
            1f, 0f, 0f, 1f,
            1f, 0f, 0f, 1f,
            1f, 0f, 0f, 1f,
            1f, 0f, 0f, 1f,
            1f, 0f, 0f, 1f,
            1f, 0f, 0f, 1f,

            // Back -> Green
            0f, 1f, 0f, 1f,
            0f, 1f, 0f, 1f,
            0f, 1f, 0f, 1f,
            0f, 1f, 0f, 1f,
            0f, 1f, 0f, 1f,
            0f, 1f, 0f, 1f,

            // Left -> Blue
            0f, 0f, 1f, 1f,
            0f, 0f, 1f, 1f,
            0f, 0f, 1f, 1f,
            0f, 0f, 1f, 1f,
            0f, 0f, 1f, 1f,
            0f, 0f, 1f, 1f,

            // Right -> Yellow
            1f, 1f, 0f, 1f,
            1f, 1f, 0f, 1f,
            1f, 1f, 0f, 1f,
            1f, 1f, 0f, 1f,
            1f, 1f, 0f, 1f,
            1f, 1f, 0f, 1f,

            // Top -> Cyan
            0f, 1f, 1f, 1f,
            0f, 1f, 1f, 1f,
            0f, 1f, 1f, 1f,
            0f, 1f, 1f, 1f,
            0f, 1f, 1f, 1f,
            0f, 1f, 1f, 1f,

            // Bottom -> Magenta
            1f, 0f, 1f, 1f,
            1f, 0f, 1f, 1f,
            1f, 0f, 1f, 1f,
            1f, 0f, 1f, 1f,
            1f, 0f, 1f, 1f,
            1f, 0f, 1f, 1f
        )
    }
}