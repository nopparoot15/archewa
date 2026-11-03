using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	public class ObjectPool<T> where T : Component, IPoolable
	{
		private readonly Func<T> factory;
		private readonly Stack<T> free = new Stack<T>();

		public ObjectPool(Func<T> factory)
		{
			this.factory = factory;
		}

		public T Get(Vector3 position, Quaternion rotation)
		{
			T item = free.Count > 0 ? free.Pop() : factory();
			item.transform.SetPositionAndRotation(position, rotation);
			item.gameObject.SetActive(true);
			item.OnSpawn();
			return item;
		}

		public void Release(T item)
		{
			if (!item.gameObject.activeSelf)
			{
				return; // already released
			}
			item.OnDespawn();
			item.gameObject.SetActive(false);
			free.Push(item);
		}
	}
}
