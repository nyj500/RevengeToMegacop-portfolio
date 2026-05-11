using System.Collections.Generic;
using UnityEngine;

namespace Boss3
{
    public class ViewController : MonoBehaviour
    {
        [SerializeField] private int _smokeCount = 0;

        [Header("Player View Hide Setting")]
        [Tooltip("플레이어가 연막 안에 들어갔을 때 메인 카메라에서 숨길 레이어들입니다.")]
        [SerializeField] private LayerMask _beHideLayers;

        [Header("Enemy Hide Layer Setting")]
        [Tooltip("적이 연막 안에 들어갔을 때 변경될 숨김용 레이어 번호입니다. (예: SmokeHide)")]    
        [SerializeField] private int _hideLayer;

        [Tooltip("적이 연막 밖으로 나왔을 때 되돌아갈 원래 레이어 번호입니다. (예: Enemy)")]
        [SerializeField] private int _originalEnemyLayer;

        private Camera _mainCamera;


        private Dictionary<GameObject, int> _enemyDic = new Dictionary<GameObject, int>();

        [Header("스모크 페널")]
        [SerializeField]
        private SmokePanel _smokePanel;

        private void Awake()
        {
            _smokePanel = gameObject.GetComponent<SmokePanel>();
            _mainCamera = Camera.main;
            if(_mainCamera != null)
            _mainCamera.cullingMask &= ~(1<<_hideLayer);

        }

        public void LimitPlayerView(bool isActive)
        {
            if (isActive)
            {
                _smokeCount++;

                if (_smokeCount > 0)
                {
                    MainCameraLayerSet(_beHideLayers, false);
                    _smokePanel.Show();
                }
            }
            else
            {
                _smokeCount = Mathf.Max(0, _smokeCount - 1);

                if (_smokeCount == 0)
                {
                    MainCameraLayerSet(_beHideLayers, true);
                    _smokePanel.Hide();
                }
            }
        }

        public void HideEnemy(bool isActive, GameObject enterObject)
        {
            if (enterObject == null) return;
            if (!enterObject.CompareTag("Enemy")) return;

            if (!_enemyDic.ContainsKey(enterObject))
            {
                _enemyDic.Add(enterObject, 0);
            }

            if (isActive)
            {
                _enemyDic[enterObject]++;

                if (_enemyDic[enterObject] > 0)
                {
                    SetTargetLayer(enterObject, _hideLayer);
                }
            }
            else
            {
                _enemyDic[enterObject] = Mathf.Max(0, _enemyDic[enterObject] - 1);

                if (_enemyDic[enterObject] == 0)
                {
                    SetTargetLayer(enterObject, _originalEnemyLayer);
                }
            }
        }

        private void SetTargetLayer(GameObject target, int currentLayer)
        {
            target.layer = currentLayer;

            foreach (Transform child in target.transform)
            {
                SetTargetLayer(child.gameObject, currentLayer);
            }
        }

        private void MainCameraLayerSet(LayerMask layers, bool isActiveLayers)
        {
            if (_mainCamera == null) return;

            if (isActiveLayers)
            {
                _mainCamera.cullingMask |= layers.value;
            }
            else
            {
                _mainCamera.cullingMask &= ~layers.value;
            }
        }
    }
}