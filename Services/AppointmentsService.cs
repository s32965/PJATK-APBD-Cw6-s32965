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

    public async Task<AppointmentDetailsDto> CreateAsync(CreateAppointmentRequestDto appointmentRequest,
        CancellationToken cancellationToken)
    {
        if (appointmentRequest.AppointmentDate < DateTime.Now)
        {
            throw new DateInvalidException("The appointment date cannot be in the past");
        }

        if (appointmentRequest.Reason is null || appointmentRequest.Reason.IsWhiteSpace())
        {
            throw new EmptyReasonException("The appointment reason must not be empty");
        }

        if (appointmentRequest.Reason.Length > 250)
        {
            throw new ReasonTooLongException("The appointment reason is too long, length cannot be more than 250 characters");
        }
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        
        await connection.OpenAsync(cancellationToken);
        
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        command.Connection = connection;
        command.Transaction = (SqlTransaction)transaction;

        try
        {
            command.CommandText = """
                                  SELECT FirstName, LastName, Email, IsActive
                                  FROM ClinicAdoNet.dbo.Patients
                                  WHERE IdPatient = @id
                                  """;
            command.Parameters.AddWithValue("@id", appointmentRequest.IdPatient);

            await using var patientReader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await patientReader.ReadAsync(cancellationToken))
            {
                throw new PatientNotFoundException($"Patient with id: {appointmentRequest.IdPatient} not found");
            }

            string patientFirstName = patientReader.GetString(patientReader.GetOrdinal("FirstName"));
            string patientLastName = patientReader.GetString(patientReader.GetOrdinal("LastName"));
            string patientEmail = patientReader.GetString(patientReader.GetOrdinal("Email"));
            bool patientIsActive = patientReader.GetBoolean(patientReader.GetOrdinal("IsActive"));

            if (!patientIsActive)
            {
                throw new PatientNotActiveException($"Patient with id: {appointmentRequest.IdPatient} is not active");
            }

            await patientReader.CloseAsync();
            command.Parameters.Clear();

            // ----------------------------------------------------------------

            command.CommandText = """
                                  SELECT D.FirstName, D.LastName, S.Name AS SpecializationName, D.IsActive
                                  FROM ClinicAdoNet.dbo.Doctors D
                                  JOIN ClinicAdoNet.dbo.Specializations S on D.IdSpecialization = S.IdSpecialization
                                  WHERE IdDoctor = @id
                                  """;
            command.Parameters.AddWithValue("@id", appointmentRequest.IdDoctor);

            await using var doctorReader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await doctorReader.ReadAsync(cancellationToken))
            {
                throw new DoctorNotFoundException($"Doctor with id: {appointmentRequest.IdDoctor} not found");
            }

            string doctorFirstName = doctorReader.GetString(doctorReader.GetOrdinal("FirstName"));
            string doctorLastName = doctorReader.GetString(doctorReader.GetOrdinal("LastName"));
            string doctorSpecializationName = doctorReader.GetString(doctorReader.GetOrdinal("SpecializationName"));
            bool doctorIsActive = doctorReader.GetBoolean(doctorReader.GetOrdinal("IsActive"));

            if (!doctorIsActive)
            {
                throw new DoctorNotActiveException($"Doctor with id: {appointmentRequest.IdDoctor} is not active");
            }

            await doctorReader.CloseAsync();
            command.Parameters.Clear();

            // --------------------------------

            command.CommandText = "SELECT 1 FROM ClinicAdoNet.dbo.Appointments WHERE IdDoctor = @IdDoctor AND AppointmentDate = @AppointmentDate AND Status != 'Cancelled'";
            command.Parameters.AddWithValue("@IdDoctor", appointmentRequest.IdDoctor);
            command.Parameters.AddWithValue("@AppointmentDate", appointmentRequest.AppointmentDate);

            var conflictExists = await command.ExecuteScalarAsync(cancellationToken);
            if (conflictExists is not null)
            {
                throw new DateConflictException($"Doctor already has an appointment scheduled at {appointmentRequest.AppointmentDate}");
            }
            command.Parameters.Clear();
            
            // --------------------------------
            
            var status = "Scheduled";
            var createdAt = DateTime.Now;

            command.CommandText = """
                                  INSERT INTO ClinicAdoNet.dbo.Appointments (IdPatient, IdDoctor, AppointmentDate, Reason, Status, CreatedAt, InternalNotes)
                                  OUTPUT inserted.IdAppointment
                                  VALUES (@IdPatient, @IdDoctor, @AppointmentDate, @Reason, @Status, @CreatedAt, '')
                                  """;

            command.Parameters.AddWithValue("@IdPatient", appointmentRequest.IdPatient);
            command.Parameters.AddWithValue("@IdDoctor", appointmentRequest.IdDoctor);
            command.Parameters.AddWithValue("@AppointmentDate", appointmentRequest.AppointmentDate);
            command.Parameters.AddWithValue("@Reason", appointmentRequest.Reason);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@CreatedAt", createdAt);

            var appointmentId = await command.ExecuteNonQueryAsync(cancellationToken);
            command.Parameters.Clear();

            await transaction.CommitAsync(cancellationToken);

            return new AppointmentDetailsDto
            {
                IdAppointment = (int)appointmentId!,
                AppointmentDate = appointmentRequest.AppointmentDate,
                Status = status,
                Reason = appointmentRequest.Reason,
                InternalNotes = string.Empty,
                CreatedAt = createdAt,
                IdPatient = appointmentRequest.IdPatient,
                IdDoctor = appointmentRequest.IdDoctor,
                PatientFirstName = patientFirstName,
                PatientLastName = patientLastName,
                PatientEmail = patientEmail,
                DoctorFirstName = doctorFirstName,
                DoctorLastName = doctorLastName,
                DoctorSpecializationName = doctorSpecializationName,
            };
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}