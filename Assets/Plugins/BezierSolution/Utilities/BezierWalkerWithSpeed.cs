using UnityEngine;
using UnityEngine.Events;

namespace BezierSolution
{
	public class BezierWalkerWithSpeed : MonoBehaviour, IBezierWalker
	{
		public delegate Quaternion RotationFilter(Quaternion suggestedRotation, float progress);
		
		public enum TravelMode { Once, Loop, PingPong }

		public RotationFilter RotationFilterOverride { get; set; }
		
		Transform cachedTransform;
		[SerializeField] BezierSpline spline;
		public TravelMode travelMode;

		public float speed = 5f;
		float progress;

        public BezierSpline Spline
        {
            get { return spline; }
            set
            {
                this.spline = value;
                this.progress = 0;
                this.onPathCompletedCalledAt0 = false;
                this.onPathCompletedCalledAt1 = false;
            }
        }

		public float NormalizedT
		{
			get { return progress; }
            set
            {
                progress = value; 
                this.onPathCompletedCalledAt0 = false;
                this.onPathCompletedCalledAt1 = false;
            }
		}

		//public float movementLerpModifier = 10f;
		public float rotationLerpModifier = 10f;

		public bool lookForward = true;

		bool isGoingForward = true;
		public bool MovingForward { get { return ( speed > 0f ) == isGoingForward; } }

		public UnityEvent onPathCompleted = new UnityEvent();
		bool onPathCompletedCalledAt1;
		bool onPathCompletedCalledAt0;

		void Awake()
		{
			cachedTransform = transform;
		}

		void Update()
		{
            if (!spline)
            {
                return;
            }

			float targetSpeed = ( isGoingForward ) ? speed : -speed;

			Vector3 targetPos = spline.MoveAlongSpline( ref progress, targetSpeed * Time.deltaTime );

			cachedTransform.position = targetPos;

			bool movingForward = MovingForward;

			if( lookForward )
			{
				Quaternion targetRotation;
				if( movingForward )
					targetRotation = Quaternion.LookRotation( spline.GetTangent( progress ) );
				else
					targetRotation = Quaternion.LookRotation( -spline.GetTangent( progress ) );

				targetRotation = FilterRotation(targetRotation, progress);
				cachedTransform.rotation = targetRotation;
			}

			if( movingForward )
			{
				if( progress >= 1f )
				{
					if( !onPathCompletedCalledAt1 )
					{
						onPathCompleted.Invoke();
						onPathCompletedCalledAt1 = true;
					}

					if( travelMode == TravelMode.Once )
						progress = 1f;
					else if( travelMode == TravelMode.Loop )
						progress -= 1f;
					else
					{
						progress = 2f - progress;
						isGoingForward = !isGoingForward;
					}
				}
				else
				{
					onPathCompletedCalledAt1 = false;
				}
			}
			else
			{
				if( progress <= 0f )
				{
					if( !onPathCompletedCalledAt0 )
					{
						onPathCompleted.Invoke();
						onPathCompletedCalledAt0 = true;
					}

					if( travelMode == TravelMode.Once )
						progress = 0f;
					else if( travelMode == TravelMode.Loop )
						progress += 1f;
					else
					{
						progress = -progress;
						isGoingForward = !isGoingForward;
					}
				}
				else
				{
					onPathCompletedCalledAt0 = false;
				}
			}
		}

		Quaternion FilterRotation(Quaternion targetRotation, float f)
		{
			var lerp = Quaternion.Lerp(cachedTransform.rotation, targetRotation, rotationLerpModifier * Time.deltaTime);			
			return RotationFilterOverride?.Invoke(lerp, f) ?? lerp;
		}
	}
}