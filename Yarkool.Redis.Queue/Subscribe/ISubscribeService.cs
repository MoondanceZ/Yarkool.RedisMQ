﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yarkool.Redis.Queue
{
    public interface ISubscribeService
    {
        /// <summary>
        /// 收到消息
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        Action<TMessage> OnMessage<TMessage>() where TMessage : BaseMessage;

        /// <summary>
        /// 发生错误
        /// </summary>
        /// <returns></returns>
        Action OnError();
    }
}
