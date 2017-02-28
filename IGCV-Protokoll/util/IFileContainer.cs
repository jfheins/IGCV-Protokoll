using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGCV_Protokoll.Models;

namespace IGCV_Protokoll.util
{
	interface IFileContainer
	{
		DocumentContainer Documents { get; set; }
		int DocumentsID { get; set; }
	}
}
