using AasxServerDB.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerDB
{
    public class PageRetriever
    {
        static public List<AASXSet> GetPageAASXData(int size = 1000, string searchLower = "", long aasxid = 0)
        {
            return new AasContext().AASXSets
                .OrderBy(a => a.Id)
                .Where(a => (aasxid == 0 || a.Id == aasxid) &&
                (searchLower.IsNullOrEmpty() || a.AASX.ToLower().Contains(searchLower)))
                .Take(size)
                .ToList();
        }

        static public List<AASSet> GetPageAASData(int size = 1000, string searchLower = "", long aasxid = 0, long aasid = 0)
        {
            return new AasContext().AASSets
                .OrderBy(a => a.Id)
                .Where(a => (aasxid == 0 || a.AASXId == aasxid) && (aasid == 0 || a.Id == aasid) &&
                    (searchLower.IsNullOrEmpty() || a.IdShort.ToLower().Contains(searchLower) || (a.Identifier == null || a.Identifier.ToLower().Contains(searchLower)) || a.AssetKind.ToLower().Contains(searchLower) || a.GlobalAssetId.ToLower().Contains(searchLower)))
                .Take(size)
                .ToList();
        }

        static public List<SMSet> GetPageSMData(int size = 1000, string searchLower = "", long aasxid = 0, long aasid = 0, long smid = 0)
        {
            return new AasContext().SMSets
                .OrderBy(s => s.Id)
                .Where(s => (aasxid == 0 || s.AASXId == aasxid) && (aasid == 0 || s.AASId == aasid) && (smid == 0 || s.Id == smid) &&
                    (searchLower.IsNullOrEmpty() || s.Identifier.ToLower().Contains(searchLower) || s.IdShort.ToLower().Contains(searchLower) || s.SemanticId.ToLower().Contains(searchLower)))
                .Take(size)
                .ToList();
        }

        static public List<SMESet> GetPageSMEData(int size = 1000, string searchLower = "", long smid = 0, long smeid = 0)
        {
            List<SMESet> data = null;
            using (AasContext db = new AasContext())
            {
                data = db.SMESets
                    .OrderBy(sme => sme.Id)
                    .Where(sme => (smid == 0 || sme.SMId == smid) && (smeid == 0 || sme.Id == smeid) &&
                        (searchLower.IsNullOrEmpty() || sme.IdShort.ToLower().Contains(searchLower) || sme.SemanticId.ToLower().Contains(searchLower) || sme.SMEType.ToLower().Contains(searchLower) || sme.ValueType.ToLower().Contains(searchLower) ||
                        (sme.ValueType != null && (
                            (sme.ValueType.Equals("S") && db.SValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToLower().Contains(searchLower))))) ||
                            (sme.ValueType.Equals("I") && db.IValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower))))) ||
                            (sme.ValueType.Equals("F") && db.DValueSets.Any(sv => sv.SMEId == sme.Id && ((sv.Annotation != null && sv.Annotation.ToLower().Contains(searchLower)) || (sv.Value != null && sv.Value.ToString().ToLower().Contains(searchLower)))))))))
                    .Take(size)
                    .ToList();
            }
            return data;
        }

        static public List<SValueSet> GetPageSValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            return new AasContext().SValueSets
                .OrderBy(v => v.SMEId)
                .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() || v.Value.ToLower().Contains(searchLower) || v.Annotation.ToLower().Contains(searchLower)))
                .Take(size)
                .ToList();
        }

        static public List<IValueSet> GetPageIValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            long iEqual = 0;
            try
            {
                iEqual = Convert.ToInt64(searchLower);
            }
            catch { }

            return new AasContext().IValueSets
                .OrderBy(v => v.SMEId)
                .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() || v.Annotation.ToLower().Contains(searchLower)) &&
                    (iEqual == 0 || v.Value == iEqual))
                .Take(size)
                .ToList();
        }

        static public List<DValueSet> GetPageDValueData(int size = 1000, string searchLower = "", long smeid = 0)
        {
            double fEqual = 0;
            try
            {
                fEqual = Convert.ToDouble(searchLower);
            }
            catch { }

            return new AasContext().DValueSets
                .OrderBy(v => v.SMEId)
                .Where(v => (smeid == 0 || v.SMEId == smeid) &&
                    (searchLower.IsNullOrEmpty() || v.Annotation.ToLower().Contains(searchLower)) &&
                    (fEqual == 0 || v.Value == fEqual))
                .Take(size)
                .ToList();
        }
    }
}
