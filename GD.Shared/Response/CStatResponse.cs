using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GD.Shared.Response;

public class CStatResponse
{
	public DateTime Date { get; set; }
	public int DeleveredAmount { get; set; }
	public double TotalPrice { get; set; }
}
