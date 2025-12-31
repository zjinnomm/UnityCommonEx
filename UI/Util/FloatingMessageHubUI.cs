using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{

    public class FloatingMessageHubUI : SingletonController<FloatingMessageHubUI>
    {
        public GameObject MessagePrefab;
        
        List<FloatingMessageController> floatingMessages = new List<FloatingMessageController>();
        Dictionary<FloatingMessageController, uint> messageTimers = new Dictionary<FloatingMessageController, uint>();
        CanvasScaler canvasScaler;

        protected override void OnActivate()
        {
            base.OnActivate();
            canvasScaler = GetComponentInParent<CanvasScaler>();
        } 

        public void MessageWorld(FloatingMessageContent content, Vector2 worldPos)
        {
            Message(content, Camera.main.WorldToViewportPoint(worldPos));
        }

        public void Message(FloatingMessageContent content, Vector2 relativePos)
        {
            FloatingMessageController message = InstancePool<FloatingMessageController>.Instance.GetInstance(() => Create<FloatingMessageController>(MessagePrefab, transform));
            Vector2 canvasSize = canvasScaler.referenceResolution;
            Vector2 position = new Vector2(relativePos.x * canvasSize.x, relativePos.y * canvasSize.y);
            message.transform.localPosition = position;
            message.transform.localScale = Vector3.one;
            message.SetContent(content);
            
            // SetContent 结束后启动 UITween
            message.StartTween();
            
            floatingMessages.Add(message);

            // 如果 Duration > 0，注册定时器自动回收
            if (message.Duration > 0f)
            {
                uint timerId = TimerManager.Start(() => ReturnMessage(message), message.Duration);
                messageTimers[message] = timerId;
            }
        }

        private void ReturnMessage(FloatingMessageController message)
        {
            if (message == null) return;
            
            // 从列表中移除
            floatingMessages.Remove(message);
            
            // 取消定时器（如果存在）
            if (messageTimers.TryGetValue(message, out uint timerId))
            {
                TimerManager.Cancel(timerId);
                messageTimers.Remove(message);
            }
            
            // 回收到对象池
            InstancePool<FloatingMessageController>.Instance.ReturnInstance(message);
        }

        public void ClearMessages()
        {
            foreach (FloatingMessageController message in floatingMessages)
            {
                // 取消定时器（如果存在）
                if (messageTimers.TryGetValue(message, out uint timerId))
                {
                    TimerManager.Cancel(timerId);
                    messageTimers.Remove(message);
                }
                
                InstancePool<FloatingMessageController>.Instance.ReturnInstance(message);
            }
            floatingMessages.Clear();
            messageTimers.Clear();
        }

        /// <summary>
        /// Cheat命令：调试显示浮动消息
        /// </summary>
        [Cheat]
        private static void DebugFloatingMessage(float x, float y, string text)
        {
            if (Instance == null)
            {
                CheatConsole.LogError("FloatingMessageHubUI instance not found.");
                return;
            }

            FloatingMessageContent content = new FloatingMessageContent
            {
                Icon = null,
                MainText = text,
                SubText = string.Empty,
                TextColor = Color.white
            };

            Vector2 relativePos = new Vector2(x, y);
            Instance.Message(content, relativePos);
        }
    }

}