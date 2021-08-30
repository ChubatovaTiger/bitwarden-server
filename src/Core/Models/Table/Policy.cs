﻿using System;
using Bit.Core.Enums;
using Bit.Core.Utilities;
using Newtonsoft.Json;

namespace Bit.Core.Models.Table
{
    public class Policy : ITableObject<Guid>
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public PolicyType Type { get; set; }
        public string Data { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreationDate { get; internal set; } = DateTime.UtcNow;
        public DateTime RevisionDate { get; internal set; } = DateTime.UtcNow;

        public void SetNewId()
        {
            Id = CoreHelpers.GenerateComb();
        }

        public T getDataModel<T>()
        {
            if (Data == null)
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(Data);
        }
    }
}
