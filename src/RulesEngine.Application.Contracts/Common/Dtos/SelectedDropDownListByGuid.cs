using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RulesEngine.Common.Dtos
{
    public class SelectedDropDownListByGuid
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public bool Disabled { get; private set; }

        public SelectedDropDownListByGuid(Guid id, string name, bool disabled)
        {
            Id = id;
            Name = name;
            Disabled = disabled;
        }
        public SelectedDropDownListByGuid(Guid id, string name)
        {
            Id = id;
            Name = name;

        }
    }
}
