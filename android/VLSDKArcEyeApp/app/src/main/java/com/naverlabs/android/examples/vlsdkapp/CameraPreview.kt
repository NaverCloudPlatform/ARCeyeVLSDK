package com.naverlabs.android.examples.vlsdkarceyeapp

import android.opengl.GLES11Ext
import android.opengl.GLES30
import android.opengl.Matrix
import android.util.Log
import java.nio.ByteBuffer
import java.nio.ByteOrder

class CameraPreview internal constructor() {
    // Shader program object
    private var shaderProgram: Int? = null

    // Handles for shader attributes/uniforms
    private var posAttrLoc: Int? = null
    private var texCoordAttrLoc: Int? = null
    private var texMatrixUniformLoc: Int? = null
    private var textureSamplerLoc: Int? = null

    fun draw(textureTransform: FloatArray, textureId: Int) {
        if(shaderProgram == null) {
            // Load & compile shaders
            val vertexShader = loadShader(
                GLES30.GL_VERTEX_SHADER,
                CAMERA_VERTEX_SHADER_SOURCE
            )
            val fragmentShader = loadShader(
                GLES30.GL_FRAGMENT_SHADER,
                CAMERA_FRAGMENT_SHADER_SOURCE
            )

            // Link program
            val id = GLES30.glCreateProgram()
            shaderProgram = id
            GLES30.glAttachShader(id, vertexShader)
            GLES30.glAttachShader(id, fragmentShader)
            GLES30.glLinkProgram(id)

            // Error check omitted (must confirm success)

            // Obtain attribute/uniform locations
            posAttrLoc = GLES30.glGetAttribLocation(id, "aPosition")
            texCoordAttrLoc = GLES30.glGetAttribLocation(id, "aTexCoord")
            texMatrixUniformLoc = GLES30.glGetUniformLocation(id, "uTexMatrix")
            textureSamplerLoc = GLES30.glGetUniformLocation(id, "sTexture")
        }

        // Use the camera shader program
        GLES30.glUseProgram(shaderProgram!!)

        // Pass the vertex/texture coordinates via VBO or a direct array
        // Here is a simple example using an array -> FloatBuffer -> glVertexAttribPointer
        val vertexBuffer = ByteBuffer
            .allocateDirect(FULLSCREEN_VERTICES.size * 4)
            .order(ByteOrder.nativeOrder())
            .asFloatBuffer()
        vertexBuffer.put(FULLSCREEN_VERTICES).position(0)
        val texCoordBuffer = ByteBuffer
            .allocateDirect(FULLSCREEN_TEXCOORDS.size * 4)
            .order(ByteOrder.nativeOrder())
            .asFloatBuffer()
        texCoordBuffer.put(FULLSCREEN_TEXCOORDS).position(0)
        GLES30.glEnableVertexAttribArray(posAttrLoc!!)
        GLES30.glVertexAttribPointer(
            posAttrLoc!!,
            3,  // Coordinate system: (x, y, z)
            GLES30.GL_FLOAT,
            false,
            0,
            vertexBuffer
        )
        GLES30.glEnableVertexAttribArray(texCoordAttrLoc!!)
        GLES30.glVertexAttribPointer(
            texCoordAttrLoc!!,
            2,  // Texture coordinates: (u, v)
            GLES30.GL_FLOAT,
            false,
            0,
            texCoordBuffer
        )

        // Pass the matrix to uTexMatrix
        GLES30.glUniformMatrix3fv(
            texMatrixUniformLoc!!,
            1,
            false,
            textureTransform,
            0
        )

        // Activate and bind the OES texture
        GLES30.glActiveTexture(GLES30.GL_TEXTURE0)
        GLES30.glBindTexture(GLES11Ext.GL_TEXTURE_EXTERNAL_OES, textureId)
        GLES30.glUniform1i(textureSamplerLoc!!, 0)

        // Draw
        GLES30.glDrawArrays(GLES30.GL_TRIANGLE_STRIP, 0, 4)

        // Cleanup
        GLES30.glDisableVertexAttribArray(posAttrLoc!!)
        GLES30.glDisableVertexAttribArray(texCoordAttrLoc!!)
    }

    /** Simple shader loader example (error checking omitted)  */
    private fun loadShader(type: Int, shaderSource: String): Int {
        val shader = GLES30.glCreateShader(type)
        GLES30.glShaderSource(shader, shaderSource)
        GLES30.glCompileShader(shader)
        return shader
    }

    companion object {
        private val TAG = CameraPreview::class.java.simpleName

        // Actual shader source (simple example)
        private const val CAMERA_VERTEX_SHADER_SOURCE = "attribute vec4 aPosition;\n" +
            "attribute vec2 aTexCoord;\n" +
            "uniform mat3 uTexMatrix;\n" +
            "varying vec2 vTexCoord;\n" +
            "void main() {\n" +
            "  gl_Position = aPosition;\n" +
            "  vec3 texTransformed = uTexMatrix * vec3(aTexCoord, 1.0);\n" +
            "  vTexCoord = texTransformed.xy;\n" +
            "}\n"
        private const val CAMERA_FRAGMENT_SHADER_SOURCE =
            "#extension GL_OES_EGL_image_external : require\n" +
            "precision mediump float;\n" +
            "uniform samplerExternalOES sTexture;\n" +
            "varying vec2 vTexCoord;\n" +
            "void main() {\n" +
            "  gl_FragColor = texture2D(sTexture, vTexCoord);\n" +
            "}\n"

        // Definition of full-screen vertices (X, Y, Z). This uses a triangle strip (two triangles) to cover the entire screen.
        private val FULLSCREEN_VERTICES = floatArrayOf(
            -1.0f, -1.0f, 0.0f,
            1.0f, -1.0f, 0.0f,
            -1.0f, 1.0f, 0.0f,
            1.0f, 1.0f, 0.0f
        )

        // Default texture coordinates (0.0 ~ 1.0)
        private val FULLSCREEN_TEXCOORDS = floatArrayOf(
            0.0f, 0.0f,
            1.0f, 0.0f,
            0.0f, 1.0f,
            1.0f, 1.0f
        )
    }
}