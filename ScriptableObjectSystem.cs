using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Green
{
    public abstract class ScriptableObjectSystem : ScriptableObject
    {
        public virtual UniTask SystemInitialize(CancellationToken token) { return UniTask.CompletedTask; }
        public virtual void SystemEnable() { }
        public virtual void SystemUpdate(float deltaTime, float unscaledDeltaTime) { }
        public virtual void SystemFixedUpdate(float deltaTime, float unscaledDeltaTime) { }
        public virtual void SystemDisable() { }
        public virtual UniTask SystemRelease(CancellationToken token) { return UniTask.CompletedTask; }
        public virtual void OnApplicationQuit() { }
    }
}
