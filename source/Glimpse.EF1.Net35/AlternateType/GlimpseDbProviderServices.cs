using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Common.CommandTrees;
using System.Linq;
using System.Text;

namespace Glimpse.EF.AlternateType
{
    public class GlimpseDbProviderServices : DbProviderServices
    {
        private readonly DbProviderServices inner;

        public GlimpseDbProviderServices(DbProviderServices inner)
        {
            if (inner == null)
                throw new ArgumentNullException("inner");

            this.inner = inner;
        }

        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            return new GlimpseDbCommandDefinition(inner.CreateCommandDefinition(commandTree));
        }

        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            return inner.GetProviderManifestToken(connection);
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            return inner.GetProviderManifest(manifestToken);
        }
    }
}
