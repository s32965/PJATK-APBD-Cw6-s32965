using System.Text;
using WebApplication1.DTOs;
using Microsoft.Data.SqlClient;

namespace WebApplication1.Services;

public class AppointmentsService(IConfiguration configuration) : IAppointmentsService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = new List<AppointmentListDto>();

        var sqlCommand = new StringBuilder("""
                                           SELECT A.IdAppointment, A.AppointmentDate, A.Status, A.Reason, P.FirstName + ' ' + P.LastName AS PatientFullName
                                           FROM Appointments A
                                           JOIN Patients P ON A.IdPatient = P.IdPatient
                                           """);
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        
        command.Connection = connection;
        command.CommandText = sqlCommand.ToString();

        await connection.OpenAsync(cancellationToken);
        
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
                AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                Reason = reader.GetString(reader.GetOrdinal("Reason")),
                PatientFullName = reader.GetString(reader.GetOrdinal("PatientFullName"))
            });
        }
        
        return result;
    }
}