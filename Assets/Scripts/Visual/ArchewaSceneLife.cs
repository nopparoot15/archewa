using UnityEngine;

namespace MyArcade
{
	public sealed class ArchewaSceneLife : MonoBehaviour
	{
		public Transform[] fans = new Transform[0];
		public Transform[] awnings = new Transform[0];
		public Light[] signs = new Light[0];
		private Quaternion[] resting;
		private float[] intensities;
		private void Awake()
		{
			resting=new Quaternion[awnings.Length];intensities=new float[signs.Length];
			for(int i=0;i<awnings.Length;i++)if(awnings[i])resting[i]=awnings[i].localRotation;
			for(int i=0;i<signs.Length;i++)if(signs[i])intensities[i]=signs[i].intensity;
		}
		private void Update()
		{
			float t=Time.time;
			for(int i=0;i<fans.Length;i++)if(fans[i])fans[i].localRotation=Quaternion.Euler(0,0,t*(210+i%3*35));
			for(int i=0;i<awnings.Length;i++)if(awnings[i])awnings[i].localRotation=resting[i]*Quaternion.Euler(Mathf.Sin(t*.8f+i)*.35f,0,0);
			for(int i=0;i<signs.Length;i++)if(signs[i])
			{
				float glow=.96f+Mathf.Sin(t*1.2f+i)*.04f;
				float cycle=Mathf.Repeat(t+i*3.17f,23);
				if(cycle>22.7f)glow*=Mathf.Sin(t*43+i)>0?.6f:.9f;
				signs[i].intensity=intensities[i]*glow;
			}
		}
		private void OnDestroy()
		{
			if(resting==null)return;
			for(int i=0;i<awnings.Length;i++)if(awnings[i])awnings[i].localRotation=resting[i];
			for(int i=0;i<signs.Length;i++)if(signs[i])signs[i].intensity=intensities[i];
		}
	}
}
