using System;
using System.Collections.Generic;
using System.Text;

namespace TanatosCognitoTrigger.Entities {
	internal class EntProfileEnviarCodigoVerificacion {
		public string? Nombre { get; set; }
		public required string CorreoElectronico { get; set; }
		public required string CodigoEncriptado { get; set; }
		public required TipoCodigoVerificacion TipoCodigo { get; set; }
	}

	internal enum TipoCodigoVerificacion {
		SignUp = 1,
		ForgotPassword = 2,
		ResendCode = 3,
		UpdateUserAttribute = 4,
		VerifyUserAttribute = 5,
		Authentication = 6,
		AdminCreateUser = 7,
		AccountTakeOverNotification = 8
	}
}
