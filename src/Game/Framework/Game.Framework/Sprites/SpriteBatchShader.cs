using System.Numerics;
using Silk.NET.OpenGL;

namespace Game.Framework.Sprites;

internal sealed class SpriteBatchShader : Shader {

	private int _projectionLocation = -1;
	private int _textureLocation = -1;

	private const string FragmentCode = """
		#version 330 core

		in vec2 frag_texCoords;
		in vec4 frag_color;

		out vec4 out_color;

		uniform sampler2D uTexture;

		void main()
		{
			vec4 tex = texture(uTexture, frag_texCoords);
			vec4 tint = vec4(frag_color.rgb * frag_color.a, frag_color.a);
			out_color = tex * tint;
		}
		""";

	private const string VertexCode = """
		#version 330 core

		layout (location = 0) in vec2 aPosition;
		layout (location = 1) in vec2 aTexCoords;
		layout (location = 2) in vec4 aColor;

		out vec2 frag_texCoords;
		out vec4 frag_color;

		uniform mat4 uProjection; 

		void main()
		{
			gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);

			frag_texCoords = aTexCoords;
			frag_color = aColor;
		}
		""";

	public SpriteBatchShader(
		GL gl
	) : base( gl, VertexCode, FragmentCode ) {

		BindUniforms();
	}

	protected override void BindUniforms() {
		_projectionLocation = GL.GetUniformLocation( Id, "uProjection" );
		_textureLocation = GL.GetUniformLocation( Id, "uTexture" );
	}

	public void Bind(
		IRenderTarget renderTarget,
		int textureUnit
	) {
		renderTarget.Bind();
		Bind();

		Matrix4x4 newOrtho = renderTarget.Projection;
		unsafe {
			GL.UniformMatrix4( _projectionLocation, 1, false, (float*)&newOrtho );
		}
		GL.Uniform1( _textureLocation, textureUnit );
	}
}
