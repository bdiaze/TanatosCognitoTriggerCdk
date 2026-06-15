using System;
using System.Collections.Generic;
using System.Text;

namespace TanatosCognitoTrigger.Entities {
	internal class EntProfileEnviarCodigoVerificacion {
		public string? Nombre { get; set; }
		public required string CorreoElectronico { get; set; }
		public required string CodigoEncriptado { get; set; }
	}
}
