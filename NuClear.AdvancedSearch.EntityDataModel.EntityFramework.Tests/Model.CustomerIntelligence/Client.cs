﻿using System.Collections.Generic;

namespace EntityDataModel.EntityFramework.Tests.Model.CustomerIntelligence
{
    public class Client
    {
        public long Id { get; set; }
        public byte CategoryGroup { get; set; }

        public ICollection<Account> Accounts { get; set; }
        public ICollection<Contact> Contacts { get; set; }
    }
}