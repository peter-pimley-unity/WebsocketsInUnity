using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour
{
	// Start is called before the first frame update

	[SerializeField]
	private float m_speed;

	private void LateUpdate()
	{
		float theta = Time.deltaTime * m_speed;
		transform.Rotate(Vector3.up, theta, Space.Self);
	}
}
