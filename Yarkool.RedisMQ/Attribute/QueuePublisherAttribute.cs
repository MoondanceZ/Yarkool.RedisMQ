﻿namespace Yarkool.RedisMQ
{
    [AttributeUsage(AttributeTargets.Class)]
    public class QueuePublisherAttribute(string queueName) : Attribute
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; private set; } = queueName;
    }
}