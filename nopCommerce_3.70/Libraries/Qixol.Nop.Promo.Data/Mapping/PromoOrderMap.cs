﻿using Qixol.Nop.Promo.Core.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qixol.Nop.Promo.Data.Mapping
{
    public class PromoOrderMap : EntityTypeConfiguration<PromoOrder>
    {
        public readonly static string TABLENAME = "PromoOrder";

        public PromoOrderMap()
        {
            this.ToTable(TABLENAME);
            this.HasKey(x => x.Id);
        }
    }
}
