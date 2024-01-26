namespace Sandbox;

public sealed class NavmeshComponent : Component
{
	public NavigationMesh NavMesh { get; private set; }

	protected override void OnAwake()
	{
		if ( Scene.IsEditor )
			return;

		Regenerate();
	}

	public void Regenerate()
	{
		if ( !Scene.Active || !Scene.PhysicsWorld.IsValid() )
			return;
		NavMesh = new();
		NavMesh.Generate( Scene.PhysicsWorld );
		Log.Info( $"Generated navmesh... Node count: {NavMesh.Nodes.Count}" );

		// TODO: When event system is re-added, use Event.Run instead??
		var agents = Scene.GetAllComponents<Agent>();
		foreach ( var agent in agents )
			agent.NavReady( NavMesh );
	}

	protected override void OnUpdate()
	{
		using ( Gizmo.Scope( "navmesh" ) )
		{
			Gizmo.Draw.Color = Color.Cyan;
			Gizmo.Draw.LineNavigationMesh( NavMesh );
		}
	}
}
