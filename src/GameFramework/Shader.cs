using Silk.NET.OpenGL;

namespace GameFramework;

public class Shader : IDisposable {
	private readonly uint _program;
	private readonly GL _gl;
	private readonly GlStateCache _stateCache;
	private bool _isDisposed;

	internal Shader(
		GL gl,
		GlStateCache stateCache,
		string vertexCode,
		string fragmentCode
	) {
		_gl = gl;
		_stateCache = stateCache;


		// Create our vertex shader, and give it our vertex shader source code.
		uint vertexShader = gl.CreateShader( ShaderType.VertexShader );
		gl.ShaderSource( vertexShader, vertexCode );

		// Attempt to compile the shader.
		gl.CompileShader( vertexShader );

		// Check to make sure that the shader has successfully compiled.
		gl.GetShader( vertexShader, ShaderParameterName.CompileStatus, out int vStatus );
		if( vStatus != (int)GLEnum.True ) {
			throw new InvalidOperationException( "Vertex shader failed to compile: " + gl.GetShaderInfoLog( vertexShader ) );
		}

		// Repeat this process for the fragment shader.
		uint fragmentShader = gl.CreateShader( ShaderType.FragmentShader );
		gl.ShaderSource( fragmentShader, fragmentCode );

		gl.CompileShader( fragmentShader );

		gl.GetShader( fragmentShader, ShaderParameterName.CompileStatus, out int fStatus );
		if( fStatus != (int)GLEnum.True ) {
			throw new InvalidOperationException( "Fragment shader failed to compile: " + gl.GetShaderInfoLog( fragmentShader ) );
		}

		// Create our shader program, and attach the vertex & fragment shaders.
		_program = gl.CreateProgram();

		gl.AttachShader( _program, vertexShader );
		gl.AttachShader( _program, fragmentShader );

		// Attempt to "link" the program together.
		gl.LinkProgram( _program );

		// Similar to shader compilation, check to make sure that the shader program has linked properly.
		gl.GetProgram( _program, ProgramPropertyARB.LinkStatus, out int lStatus );
		if( lStatus != (int)GLEnum.True ) {
			throw new InvalidOperationException( "Program failed to link: " + gl.GetProgramInfoLog( _program ) );
		}

		// Detach and delete our shaders. Once a program is linked, we no longer need the individual shader objects.
		gl.DetachShader( _program, vertexShader );
		gl.DetachShader( _program, fragmentShader );
		gl.DeleteShader( vertexShader );
		gl.DeleteShader( fragmentShader );
	}

	protected virtual void BindUniforms() {
	}

	internal uint Id => _program;

	protected GL GL => _gl;

	public void Bind() {
		_stateCache.UseProgram( _program );
	}

	public void Dispose() {
		if( _isDisposed ) {
			return;
		}

		_gl.DeleteProgram( _program );
		_isDisposed = true;
		GC.SuppressFinalize( this );
	}

	~Shader() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( "Shader leaked. Dispose it on the render thread." );
		}
	}
}
