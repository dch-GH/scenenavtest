namespace Sandbox;

public sealed class PanningCameraController : Component
{
	[Property] private float MoveSpeed { get; set; }
	private Vector3 _move;
	protected override void OnUpdate()
	{
		_move = 0;
		var rot = GameObject.Transform.Rotation;

		if ( Input.Down( "Forward" ) ) _move += rot.Forward;
		if ( Input.Down( "Backward" ) ) _move += rot.Backward;
		if ( Input.Down( "Left" ) ) _move += rot.Left;
		if ( Input.Down( "Right" ) ) _move += rot.Right;
		
		_move = _move.WithZ( 0 );

		if ( !_move.IsNearZeroLength )
			_move = _move.Normal;

		_move *= Input.Down( GameInputActions.Run ) ? MoveSpeed * 4 : MoveSpeed;
		GameObject.Transform.Position += _move * Time.Delta;
	}
}
