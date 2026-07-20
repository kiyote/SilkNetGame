using System.Drawing;

namespace GameFramework.Scenes.Tests;

[TestFixture]
internal sealed class SceneManagerTests {

	[Test]
	public void Ctor_ValidCoordinates_SceneIsDefined() {
		ISceneManager scene = new SceneManager( new Dimension( 100, 100 ) );

		Assert.That( scene.Root.Clip, Is.EqualTo( new Rectangle( 0, 0, 100, 100 ) ) );
	}

	[Test]
	public void AddScene_SceneContained_FullSceneCreated() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode scene = new SceneNode( sceneManager.Root, new Coordinate( 10, 10 ), new Dimension( 90, 90 ), NullSceneMouseHandler.Instance );

		Assert.That( scene.Clip, Is.EqualTo( new Rectangle( 10, 10, 90, 90 ) ) );
	}

	[Test]
	public void AddScene_SceneClipped_ClippedSceneReturned() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		SceneNode node1 = new SceneNode( sceneManager.Root, new Coordinate( 10, 10 ), new Dimension( 90, 90 ), NullSceneMouseHandler.Instance );
		SceneNode node2 = new SceneNode( node1, new Coordinate( 50, 50 ), new Dimension( 100, 100 ), NullSceneMouseHandler.Instance );

		Assert.That( node2.Clip, Is.EqualTo( new Rectangle( 60, 60, 40, 40 ) ) );
	}

	[Test]
	public void MouseDown_NodeInAndOut_OnlyInCodeCalled() {
		ISceneManager sceneManager = new SceneManager( new Dimension( 100, 100 ) );
		TestingNodeHandler node1Handler = new TestingNodeHandler();
		sceneManager.Root.AddChild( default, new Dimension( 50, 100 ), node1Handler );

		TestingNodeHandler node2Handler = new TestingNodeHandler();
		sceneManager.Root.AddChild( new Coordinate( 50, 0 ), new Dimension( 50, 100 ), node2Handler );

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
		sceneManager.Root.AddChild( default, new Dimension( 50, 100 ), node1Handler );

		TestingNodeHandler node2Handler = new TestingNodeHandler();
		sceneManager.Root.AddChild( new Coordinate( 50, 0 ), new Dimension( 50, 100 ), node2Handler );

		Coordinate coordinate = new Coordinate( 75, 25 );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( sceneManager.MouseUp( coordinate, 0 ), Is.True, "No node indicated it handled the mouse up event." );
			Assert.That( node1Handler.WasMouseReleasedCalled, Is.True, "Node1 did not register a mouse release." );
			Assert.That( node1Handler.WasMouseUpCalled, Is.False, "Node1 incorrectly registered a mouse up." );

			Assert.That( node2Handler.WasMouseReleasedCalled, Is.True, "Node2 did not register a mouse release." );
			Assert.That( node2Handler.WasMouseUpCalled, Is.True, "Node2 did not register a mouse up." );
		}
	}
}
