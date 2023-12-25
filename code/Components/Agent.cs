namespace Sandbox;

public sealed class Agent : Component
{
	public static float MoveSpeed = 450;

	/// <summary>
	/// Reference to the navmesh injected by <see cref="NavmeshComponent"/>,
	/// where it is generated.
	/// </summary>
	private NavigationMesh _navMesh;
	private NavigationPath _currentPath;

	/// <summary>
	/// A buffer of<see cref="NavigationPath.Segment"/>s
	/// copied from <see cref="_currentPath"/>'s segments
	/// which we can remove segments from as we reach them.
	/// </summary>
	private HashSet<NavigationPath.Segment> _buffer;

	/// <summary>
	/// The agent's ultimate destination.
	/// </summary>
	private Vector3 _destination;
	private CharacterController _cc;

	protected override void OnAwake()
	{
		_cc = GameObject.Components.Get<CharacterController>();
	}

	public void NavReady( NavigationMesh navmesh )
	{
		_navMesh = navmesh;
		_currentPath = new NavigationPath( _navMesh );
		_buffer = new();
	}

	protected override void OnUpdate()
	{
		if ( Input.Down( GameInputActions.Use ) )
		{
			_buffer.Clear();
			TryGeneratePath();
		}
		else if ( Input.Released( GameInputActions.Use ) )
		{
			_buffer.Clear();
			foreach ( var s in _currentPath.Segments )
				_buffer.Add( s );
		}

		if ( !Transform.Position.AlmostEqual( _destination, 10 ) )
		{
			Gizmo.Draw.Color = Color.Green;
			Gizmo.Draw.LineCylinder( _destination, _destination + Vector3.Up * 100, _cc.Radius, _cc.Radius, 20 );
		}

		DrawNavSegments( _buffer, Color.Blue );
	}

	protected override void OnFixedUpdate()
	{
		if ( _currentPath is null )
			return;

		if ( !Input.Down( GameInputActions.Use ) )
			TryNavigatePath();
	}

	private void TryGeneratePath()
	{
		var screenRay = Camera.Main.GetRay( Mouse.Position, Screen.Size );
		var tr = Scene.Trace.Ray( screenRay, 100000f ).UsePhysicsWorld().Run();

		if ( tr.Hit && tr.Normal != Vector3.Up )
			return;

		if ( tr.Hit )
		{
			_destination = tr.HitPosition;

			_currentPath.StartPoint = Transform.Position;
			_currentPath.EndPoint = _destination;
			_currentPath.Build();

			var debugOrigin = screenRay.Position + screenRay.Forward * 50;
			Gizmo.Draw.LineCylinder( debugOrigin, tr.HitPosition, 1, 1, 32 );
			Gizmo.Draw.Color = Color.Green;
			Gizmo.Draw.LineCylinder( tr.HitPosition, tr.HitPosition + Vector3.Up * 100, _cc.Radius, _cc.Radius, 20 );

			DrawNavSegments( _currentPath.Segments, Color.Cyan );
		}
	}

	private void TryNavigatePath()
	{
		if ( Transform.Position.AlmostEqual( _destination, 10 ) )
		{
			Transform.Position = _destination;
			return;
		}

		// NOTE: The current way s&box nav works is that it does not generate paths inside nodes, only between nodes.
		// So, just use _destination instead.
		var dest = (_buffer is not null && _buffer.Count > 0) ? _buffer.First().Position : _destination;
		var dir = (dest - Transform.Position).WithZ( 0 ).Normal;
		_cc.Velocity = dir * MoveSpeed;
		_cc.Move();

		if ( Transform.Position.AlmostEqual( dest, _cc.Radius / 2 ) )
		{
			_currentPath.StartPoint = dest;
			_currentPath.EndPoint = _destination;
			_currentPath.Build();
			_buffer.RemoveWhere( s => s.Position.AlmostEqual( Transform.Position, _cc.Radius / 2 ) );
		}
	}

	private void DrawNavSegments( IEnumerable<NavigationPath.Segment> segments, Color color )
	{
		if ( segments is null || segments.Count() <= 0 )
			return;

		for ( int i = 0; i < segments.Count() - 1; i++ )
		{
			var p1 = segments.ElementAt( i );
			var p2 = segments.ElementAt( i + 1 );

			Gizmo.Draw.Color = color;
			Gizmo.Draw.LineSphere( p1.Position, 8 );
			Gizmo.Draw.LineSphere( p2.Position, 8 );

			{
				var dir = (p2.Position - p1.Position).Normal;
				var rot = Rotation.LookAt( dir, Vector3.Up );
				using ( Gizmo.Scope( "navsegments", p2.Position, Rotation.Identity, 1.5f ) )
				{
					Gizmo.Draw.Color = Color.Green;
					Gizmo.Draw.Line( Vector3.Zero, dir + rot.Right * 27 + rot.Backward * 25 );
					Gizmo.Draw.Color = Color.Red;
					Gizmo.Draw.Line( Vector3.Zero, dir + rot.Left * 27 + rot.Backward * 25 );
				}
			}

			Gizmo.Draw.Color = Color.White;
			Gizmo.Draw.Line( p1.Position, p2.Position ); ;
		}
	}
}
