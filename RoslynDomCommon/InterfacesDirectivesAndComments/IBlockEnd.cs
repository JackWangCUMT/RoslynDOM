﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynDom.Common
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDetailBlockEnd : IDetail<IDetailBlockEnd>, IHasGroup
   {
      IDetailBlockStart BlockStart { get; set; }
      string BlockStyleName { get; }
      bool SemanticallyValid { get; }
   }
}
