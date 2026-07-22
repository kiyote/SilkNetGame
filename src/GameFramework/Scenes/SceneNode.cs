using System.Runtime.CompilerServices;
using GameFramework.Sprites;

namespace GameFramework.Scenes;

public sealed class SceneNode {

	private Coordinate _position;
	private Dimension _size;

	internal SceneNode(
		Dimension size
	) {
		_size = size;
		Handler = ISceneMouseHandler.None;
		Renderer = ISceneRenderHandler.None;
		Recalculate();
	}

	internal SceneNode(
		SceneNode parent,
		Coordinate position,
		Dimension size,
		ISceneMouseHandler handler,
		ISceneRenderHandler renderer
	) {
		Parent = parent;
		_position = position;
		_size = size;
		Handler = handler;
		Renderer = renderer;
		parent.Children.Add( this );
		Recalculate();
	}

	public SceneNode AddChild(
		Coordinate position,
		Dimension size,
		ISceneMouseHandler handler,
		ISceneRenderHandler renderer
	) {
		return new SceneNode( this, position, size, handler, renderer );
	}

	internal SceneNode? Parent { get; private set; }

	public Coordinate Position {
		get => _position;
		set {
			_position = value;
			Recalculate();
		}
	}

	public Dimension Size {
		get => _size;
		set {
			_size = value;
			Recalculate();
		}
	}

	public Bounds Clip { get; private set; }
#pragma warning disable CA1002
	public List<SceneNode> Children { get; } = [];
#pragma warning restore CA1002


	public ISceneMouseHandler Handler { get; set; }

	public ISceneRenderHandler Renderer { get; set; }

	internal void Reparent(
		SceneNode newParent
	) {
		ArgumentNullException.ThrowIfNull( newParent );
		if( Parent is null ) {
			throw new InvalidOperationException( "The root scene node cannot be re-parented." );
		}
		if( ReferenceEquals( newParent, this ) || IsAncestorOf( newParent ) ) {
			throw new InvalidOperationException( "A scene node cannot be re-parented beneath itself or one of its descendants." );
		}

		_ = Parent.Children.Remove( this );
		Parent = newParent;
		newParent.Children.Add( this );
		Recalculate();
	}

	internal void Reorder(
		int index
	) {
		if( Parent is null ) {
			throw new InvalidOperationException( "The root scene node cannot be re-ordered." );
		}

		List<SceneNode> siblings = Parent.Children;
		if( !siblings.Remove( this ) ) {
			throw new InvalidOperationException( "The scene node is not a child of its parent." );
		}
		index = Math.Clamp( index, 0, siblings.Count );
		siblings.Insert( index, this );
	}

	internal void ReorderBefore(
		SceneNode before
	) {
		ArgumentNullException.ThrowIfNull( before );
		if( Parent is null ) {
			throw new InvalidOperationException( "The root scene node cannot be re-ordered." );
		}
		if( ReferenceEquals( before, this ) ) {
			throw new InvalidOperationException( "A scene node cannot be re-ordered relative to itself." );
		}
		if( !ReferenceEquals( before.Parent, Parent ) ) {
			throw new InvalidOperationException( "A scene node can only be re-ordered relative to one of its siblings." );
		}

		List<SceneNode> siblings = Parent.Children;
		_ = siblings.Remove( this );
		int index = siblings.IndexOf( before );
		siblings.Insert( index, this );
	}

	private bool IsAncestorOf(
		SceneNode node
	) {
		SceneNode? current = node.Parent;
		while( current is not null ) {
			if( ReferenceEquals( current, this ) ) {
				return true;
			}
			current = current.Parent;
		}
		return false;
	}

	private void Recalculate() {
		if( Parent is null ) {
			Clip = new Bounds( 0, 0, _size.Width, _size.Height );
		} else {
			Bounds r = new Bounds( Parent.Clip.X + _position.X, Parent.Clip.Y + _position.Y, _size.Width, _size.Height );
			Clip = Bounds.Intersect( Parent.Clip, r );
		}

		foreach( SceneNode child in Children ) {
			child.Recalculate();
		}
	}

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

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void Update(
		double deltaTime
	) =>
		Renderer.OnUpdate( this, deltaTime );

}
