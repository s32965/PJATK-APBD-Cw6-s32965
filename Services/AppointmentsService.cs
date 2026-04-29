using System.Text;
using WebApplication1.DTOs;
using Microsoft.Data.SqlClient;
using WebApplication1.Exceptions;

namespace WebApplication1.Services;

public class AppointmentsService(IConfiguration configuration) : IAppointmentsService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? patientLastName, string? status, CancellationToken cancellationToken)
    {
        var result = new List<AppointmentListDto>();

        var sqlCommand = new StringBuilder("""
                                           SELECT A.IdAppointment, A.AppointmentDate, A.Status, A.Reason, P.FirstName + ' ' + P.LastName AS PatientFullName
                                           FROM ClinicAdoNet.dbo.Appointments A
                                           JOIN ClinicAdoNet.dbo.Patients P ON A.IdPatient = P.IdPatient
                                           """);
        var conditions = new List<string>();
        var parameters = new List<SqlParameter>();

        if (patientLastName is not null)
        {
            conditions.Add("P.LastName = @patientLastName");
            parameters.Add(new SqlParameter("@patientLastName", patientLastName));
        }

        if (status is not null)
        {
            conditions.Add("A.Status = @status");
            parameters.Add(new SqlParameter("@status", status));
        }

        if (parameters.Count > 0)
        {
            sqlCommand.Append(" WHERE ");
            sqlCommand.Append(string.Join(" AND ", conditions));
        }
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        
        command.Connection = connection;
        command.CommandText = sqlCommand.ToString();
        command.Parameters.AddRange(parameters.ToArray());

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

    public async Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        AppointmentDetailsDto? result = null;

        var sqlCommand = new StringBuilder("""
                                           SELECT 
                                           A.IdAppointment,
                                           A.AppointmentDate,
                                           A.Status,
                                           A.Reason,
                                           A.InternalNotes,
                                           A.CreatedAt,
                                           A.IdPatient,
                                           A.IdDoctor,
                                           P.FirstName AS PatientFirstName,
                                           P.LastName AS PatientLastName,
                                           P.Email AS PatientEmail,
                                           D.FirstName AS DoctorFirstName,
                                           D.LastName AS DoctorLastName,
                                           S.Name AS SpecializationName
                                           FROM ClinicAdoNet.dbo.Appointments A
                                           JOIN ClinicAdoNet.dbo.Patients P ON A.IdPatient = P.IdPatient
                                           JOIN ClinicAdoNet.dbo.Doctors D ON A.IdDoctor = D.IdDoctor
                                           JOIN ClinicAdoNet.dbo.Specializations S ON D.IdSpecialization = S.IdSpecialization
                                           WHERE A.IdAppointment = @id
                                           """);
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        
        command.Connection = connection;
        command.CommandText = sqlCommand.ToString();
        command.Parameters.AddWithValue("@id", id);

        await connection.OpenAsync(cancellationToken);
        
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result ??= new AppointmentDetailsDto
            {
                IdAppointment = reader.GetInt32(reader.GetOrdinal("IdAppointment")),
                AppointmentDate = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                Reason = reader.GetString(reader.GetOrdinal("Reason")),
                InternalNotes = reader.IsDBNull(4) ? null : reader.GetString(reader.GetOrdinal("InternalNotes")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                IdPatient = reader.GetInt32(reader.GetOrdinal("IdPatient")),
                IdDoctor = reader.GetInt32(reader.GetOrdinal("IdDoctor")),
                PatientFirstName = reader.GetString(reader.GetOrdinal("PatientFirstName")),
                PatientLastName = reader.GetString(reader.GetOrdinal("PatientLastName")),
                PatientEmail =  reader.GetString(reader.GetOrdinal("PatientEmail")),
                DoctorFirstName = reader.GetString(reader.GetOrdinal("DoctorFirstName")),
                DoctorLastName = reader.GetString(reader.GetOrdinal("DoctorLastName")),
                DoctorSpecializationName = reader.GetString(reader.GetOrdinal("SpecializationName")),
            };
        }

        if (result is null)
        {
            throw new NotFoundException($"Appointment with id {id} not found");
        }
        
        return result;
    }
}