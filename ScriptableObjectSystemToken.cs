using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Green
{
    public abstract class ScriptableObjectSystemToken<T> : BaseScriptableObjectSystemToken
        where T : ScriptableObjectSystem
    {
        [ShowInInspector, ReadOnly] public bool IsInitialized => Instance != null;

        [System.NonSerialized, HideInEditorMode, ShowInInspector, ReadOnly, InlineEditor]
        public T Instance;

        public override async UniTask WaitInitialized(CancellationToken token)
        {
            await UniTask.WaitUntil(() => IsInitialized, cancellationToken: token);
        }

        public override Object CreateInstance()
        {
            Instance = Instantiate(Original) as T;
            return Instance;
        }

        public override void Release()
        {
            Destroy(Instance);
            Instance = null;
        }
    }

    public abstract class BaseScriptableObjectSystemToken : ScriptableObject
    {
        [HideInPlayMode] public ScriptableObjectSystem Original;
        public abstract Object CreateInstance();
        public abstract void Release();
        public abstract UniTask WaitInitialized(CancellationToken token);
    }
}
