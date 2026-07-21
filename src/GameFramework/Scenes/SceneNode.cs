using System.Runtime.CompilerServices;
using GameFramework.Sprites;

namespace GameFramework.Scenes;

public sealed class SceneNode {
	internal SceneNode(
		Dimension size
	) {
		Clip = new Bounds( 0, 0, size.Width, size.Height );
		Size = size;
		Handler = ISceneMouseHandler.None;
		Renderer = ISceneRenderHandler.None;
	}

	internal SceneNode(
		SceneNode parent,
		Coordinate position,
		Dimension size,
		ISceneMouseHandler handler,
		ISceneRenderHandler renderer
	) {
		Bounds r = new Bounds( parent.Clip.X + position.X, parent.Clip.Y + position.Y, size.Width, size.Height );
		Clip = Bounds.Intersect( parent.Clip, r );
		Position = position;
		Size = new Dimension( Clip.Width, Clip.Height );
		Handler = handler;
		Renderer = renderer;
		parent.Children.Add( this );
	}

	public void AddChild(
		Coordinate position,
		Dimension size,
		ISceneMouseHandler handler,
		ISceneRenderHandler renderer
	) {
		SceneNode child = new SceneNode( this, position, size, handler, renderer );

		Children.Add( child );
	}

	public Coordinate Position { get; }
	public Dimension Size { get; }
	public Bounds Clip { get; }
#pragma warning disable CA1002
	public List<SceneNode> Children { get; } = [];
#pragma warning restore CA1002


	public ISceneMouseHandler Handler { get; set; }

	public ISceneRenderHandler Renderer { get; set; }

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public bool MouseUp(
		Coordinate coordinate,
		int button,
		bool isHandled
	) =>
		  Handler.OnMouseUp( this, coordinate, button, isHandled );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MousePressed(
		int button
	) =>
		Handler.OnMousePressed( this, button );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public bool MouseDown(
		Coordinate coordinate,
		int button,
		bool isHandled
	) =>
		  Handler.OnMouseDown( this, coordinate, button, isHandled );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MouseReleased(
		int button
	) =>
		Handler.OnMouseReleased( this, button );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MouseMoved(
		Coordinate coordinate
	) =>
		Handler.OnMouseMoved( this, coordinate );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public bool MouseMove(
		Coordinate coordinate,
		bool isHandled
	) =>
		  Handler.OnMouseMove( this, coordinate, isHandled );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MouseEntered(
	) =>
		Handler.OnMouseEntered( this );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MouseExited(
	) =>
		Handler.OnMouseExited( this );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void Render(
		ISpriteBatch spriteBatch
	) =>
		Renderer.OnRender( this, spriteBatch );

}
