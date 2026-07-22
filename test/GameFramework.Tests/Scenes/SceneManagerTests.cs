namespace GameFramework.Scenes.Tests;

[TestFixture]
internal sealed class SceneManagerTests {

	[Test]
	public void Ctor_ValidCoordinates_SceneIsDefined() {
		ISceneManager scene = new SceneManager( new Dimension( 100, 100 ) );

		Assert.That( scene.Root.Clip, Is.EqualTo( new Bounds( 0, 0, 100, 100 ) ) );
	}

	[Test]
	public void AddScene_SceneContained_FullSceneCreated() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode scene = new SceneNode( sceneManager.Root, new Coordinate( 10, 10 ), new Dimension( 90, 90 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		Assert.That( scene.Clip, Is.EqualTo( new Bounds( 10, 10, 90, 90 ) ) );
	}

	[Test]
	public void AddScene_SceneClipped_ClippedSceneReturned() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode node1 = new SceneNode( sceneManager.Root, new Coordinate( 10, 10 ), new Dimension( 90, 90 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode node2 = new SceneNode( node1, new Coordinate( 50, 50 ), new Dimension( 100, 100 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		Assert.That( node2.Clip, Is.EqualTo( new Bounds( 60, 60, 40, 40 ) ) );
	}

	[Test]
	public void MouseDown_NodeInAndOut_OnlyInCodeCalled() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		TestingNodeHandler node1Handler = new TestingNodeHandler();
		sceneManager.Root.AddChild( default, new Dimension( 50, 100 ), node1Handler, ISceneRenderHandler.None );

		TestingNodeHandler node2Handler = new TestingNodeHandler();
		sceneManager.Root.AddChild( new Coordinate( 50, 0 ), new Dimension( 50, 100 ), node2Handler, ISceneRenderHandler.None );

		Coordinate coordinate = new Coordinate( 75, 25 );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( sceneManager.MouseDown( coordinate, 0 ), Is.True, "No node indicated it handled the mouse down event." );
			Assert.That( node1Handler.WasMousePressedCalled, Is.True, "Node1 did not register a mouse press." );
			Assert.That( node1Handler.WasMouseDownCalled, Is.False, "Node1 incorrectly registered a mouse down." );

			Assert.That( node2Handler.WasMousePressedCalled, Is.True, "Node2 did not register a mouse press." );
			Assert.That( node2Handler.WasMouseDownCalled, Is.True, "Node2 did not register a mouse down." );
		}
	}

	[Test]
	public void MouseUp_NodeInAndOut_OnlyInCodeCalled() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		TestingNodeHandler node1Handler = new TestingNodeHandler();
		sceneManager.Root.AddChild( default, new Dimension( 50, 100 ), node1Handler, ISceneRenderHandler.None );

		TestingNodeHandler node2Handler = new TestingNodeHandler();
		sceneManager.Root.AddChild( new Coordinate( 50, 0 ), new Dimension( 50, 100 ), node2Handler, ISceneRenderHandler.None );

		Coordinate coordinate = new Coordinate( 75, 25 );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( sceneManager.MouseUp( coordinate, 0 ), Is.True, "No node indicated it handled the mouse up event." );
			Assert.That( node1Handler.WasMouseReleasedCalled, Is.True, "Node1 did not register a mouse release." );
			Assert.That( node1Handler.WasMouseUpCalled, Is.False, "Node1 incorrectly registered a mouse up." );

			Assert.That( node2Handler.WasMouseReleasedCalled, Is.True, "Node2 did not register a mouse release." );
			Assert.That( node2Handler.WasMouseUpCalled, Is.True, "Node2 did not register a mouse up." );
		}
	}

	[Test]
	public void Reparent_NodeWithChildren_MovedAndClipsRecalculated() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode oldParent = sceneManager.Root.AddChild( new Coordinate( 10, 10 ), new Dimension( 80, 80 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode newParent = sceneManager.Root.AddChild( new Coordinate( 30, 30 ), new Dimension( 60, 60 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode moving = oldParent.AddChild( new Coordinate( 5, 5 ), new Dimension( 20, 20 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode child = moving.AddChild( new Coordinate( 1, 1 ), new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		sceneManager.Reparent( moving, newParent );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( oldParent.Children, Does.Not.Contain( moving ), "Node was not removed from its old parent." );
			Assert.That( newParent.Children, Does.Contain( moving ), "Node was not added to its new parent." );
			Assert.That( newParent.Children[^1], Is.SameAs( moving ), "Node was not added as the last child." );
			Assert.That( moving.Children, Does.Contain( child ), "Child did not move with the re-parented node." );
			// newParent.Clip origin is (30,30); moving offset (5,5) => (35,35)
			Assert.That( moving.Clip, Is.EqualTo( new Bounds( 35, 35, 20, 20 ) ), "Moved node clip was not recalculated." );
			// child offset (1,1) relative to moving => (36,36)
			Assert.That( child.Clip, Is.EqualTo( new Bounds( 36, 36, 10, 10 ) ), "Descendant clip was not recalculated." );
		}
	}

	[Test]
	public void Reparent_RootNode_Throws() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode target = sceneManager.Root.AddChild( default, new Dimension( 50, 50 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		Assert.That( () => sceneManager.Reparent( sceneManager.Root, target ), Throws.InvalidOperationException );
	}

	[Test]
	public void Reparent_UnderOwnDescendant_Throws() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode parent = sceneManager.Root.AddChild( default, new Dimension( 50, 50 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode child = parent.AddChild( default, new Dimension( 25, 25 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		Assert.That( () => sceneManager.Reparent( parent, child ), Throws.InvalidOperationException );
	}

	[Test]
	public void Reorder_ValidIndex_NodeMovedToIndex() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode node0 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode node1 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode node2 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		sceneManager.Reorder( node0, 2 );

		Assert.That( sceneManager.Root.Children, Is.EqualTo( new[] { node1, node2, node0 } ) );
	}

	[Test]
	public void Reorder_IndexBeyondCount_ClampedToEnd() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode node0 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode node1 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		sceneManager.Reorder( node0, 99 );

		Assert.That( sceneManager.Root.Children, Is.EqualTo( new[] { node1, node0 } ) );
	}

	[Test]
	public void Reorder_NegativeIndex_ClampedToStart() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode node0 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode node1 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		sceneManager.Reorder( node1, -5 );

		Assert.That( sceneManager.Root.Children, Is.EqualTo( new[] { node1, node0 } ) );
	}

	[Test]
	public void ReorderBefore_Sibling_NodeMovedBefore() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode node0 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode node1 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode node2 = sceneManager.Root.AddChild( default, new Dimension( 10, 10 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		sceneManager.ReorderBefore( node2, node0 );

		Assert.That( sceneManager.Root.Children, Is.EqualTo( new[] { node2, node0, node1 } ) );
	}

	[Test]
	public void ReorderBefore_NonSibling_Throws() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode node0 = sceneManager.Root.AddChild( default, new Dimension( 50, 50 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode nested = node0.AddChild( default, new Dimension( 25, 25 ), ISceneMouseHandler.None, ISceneRenderHandler.None );
		SceneNode node1 = sceneManager.Root.AddChild( default, new Dimension( 50, 50 ), ISceneMouseHandler.None, ISceneRenderHandler.None );

		Assert.That( () => sceneManager.ReorderBefore( node1, nested ), Throws.InvalidOperationException );
	}
}
