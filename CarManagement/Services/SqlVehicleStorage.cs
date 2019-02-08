﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CarManagement.Core.Models.DTOs;
using CarManagement.Core.Models;
using CarManagement.Core.Services;

namespace CarManagement.Services
{
    public class SqlVehicleStorage : IVehicleStorage
    {
        private const string clearCommand = "DELETE FROM door; " +
            //"DBCC CHECKIDENT(door, RESEED, 0); " +
            "DELETE FROM wheel; " +
            //"DBCC CHECKIDENT (wheel, RESEED, 0); " +
            "DELETE FROM vehicle; " +
            "DELETE FROM enrollment;";
        //"DBCC CHECKIDENT (enrollment, RESEED, 0);";

        private const string INSERT_ENROLLMENT = "INSERT INTO [enrollment] (serial,number) " +
"OUTPUT INSERTED.ID " +
"VALUES (@serial, @number)";
        private const string INSERT_VEHICLE = "INSERT INTO [vehicle] (enrollmentId, color, engineHorsePower, engineIsStarted) " +
            "VALUES (@enrollmentKEY, @color, @horsepower, @started)";
        private const string INSERT_WHEEL = "INSERT INTO [wheel] (pressure,vehicleId) " +
            "VALUES (@pressure, @enrollmentKEY)";
        private const string INSERT_DOOR = "INSERT INTO [door] (isOpen, vehicleId) " +
            "VALUES (@open, @enrollmentKEY)";



        private readonly string connectionString;
        private readonly IVehicleBuilder vehicleBuilder;
        private readonly SqlConnection connection;

        public SqlVehicleStorage(string connectionString, IVehicleBuilder vehicleBuilder)
        {
            this.connectionString = connectionString;
            this.vehicleBuilder = vehicleBuilder;

            this.connection = new SqlConnection(this.connectionString);
            this.connection.Open();
        }

        public int Count { get; }

        public void clear()
        {
            SqlCommand command = new SqlCommand(clearCommand, this.connection);
            int affectedRows = command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            this.connection.Close();
        }

        public IVehicle get(IEnrollment enrollment)
        {
            string getEnrollments = "SELECT id FROM enrollment " +
                "WHERE (serial = @serial AND number = @number)";
            SqlDataReader enrollmentResults;
            int enrollmentId;

            using (SqlCommand command = new SqlCommand(getEnrollments, this.connection))
            {
                command.Parameters.AddWithValue("@serial", enrollment.Serial);
                command.Parameters.AddWithValue("@number", enrollment.Number);
                enrollmentId = Convert.ToInt32(command.ExecuteScalar());
            }

            if (enrollmentId > 0)
            {
                EnrollmentDto enrollmentdto = new EnrollmentDto();
                enrollmentdto.Number = enrollment.Number;
                enrollmentdto.Serial = enrollment.Serial;

                //int enrollmentId = (int)enrollmentResults.GetValue(0);

                string getVehicle = "SELECT color, engineHorsePower, engineIsStarted FROM vehicle " +
                    "WHERE (enrollmentId=@id)";

                SqlCommand commandVehicle = new SqlCommand(getVehicle, this.connection);
                commandVehicle.Parameters.AddWithValue("@id", enrollmentId);
                SqlDataReader vehicleResults = commandVehicle.ExecuteReader();

                vehicleResults.Read();

                if (vehicleResults.HasRows)
                {
                    VehicleDto vehicle = new VehicleDto();
                    vehicle.Enrollment = enrollmentdto;
                    CarColor color;
                    Enum.TryParse<CarColor>(vehicleResults.GetValue(0).ToString(), out color);
                    vehicle.Color = color;
                    EngineDto engine = new EngineDto();
                    engine.HorsePower = Convert.ToInt32(vehicleResults.GetValue(1));
                    engine.IsStarted = Convert.ToBoolean(vehicleResults.GetValue(2));
                    vehicle.Engine = engine;
                    string getWheels = "SELECT pressure FROM wheel " +
                        "WHERE (vehicleId=@id)";
                    SqlCommand commandWheels = new SqlCommand(getWheels, this.connection);
                    commandWheels.Parameters.AddWithValue("@id", enrollmentId);
                    SqlDataReader wheelsResults = commandWheels.ExecuteReader();

                    if (wheelsResults.HasRows)
                    {
                        List<WheelDto> wheels = new List<WheelDto>();
                        while (wheelsResults.Read())
                        {
                            WheelDto wheel = new WheelDto();
                            wheel.Pressure = Convert.ToDouble(wheelsResults.GetValue(0));
                            wheels.Add(wheel);
                        }
                        vehicle.Wheels = wheels.ToArray();
                    }

                    string getDoors = "SELECT isOpen FROM door " +
                         "WHERE (vehicleId=@id)";
                    SqlCommand commandDoors = new SqlCommand(getDoors, this.connection);
                    commandDoors.Parameters.AddWithValue("@id", enrollmentId);
                    SqlDataReader doorsResults = commandDoors.ExecuteReader();

                    if (doorsResults.HasRows)
                    {
                        List<DoorDto> doors = new List<DoorDto>();
                        while (doorsResults.Read())
                        {
                            DoorDto door = new DoorDto();
                            door.IsOpen = Convert.ToBoolean(vehicleResults.GetValue(0));
                            doors.Add(door);
                        }
                        vehicle.Doors = doors.ToArray();
                    }
                    return this.vehicleBuilder.import(vehicle);
                }
            }

            return null;
        }

        public IEnumerable<IVehicle> getAll()
        {
            List<IVehicle> vehicleCollection = new List<IVehicle>();
            string getEnrollments = "SELECT serial, number, id FROM enrollment";
            SqlDataReader enrollmentResults;

            using (SqlCommand command = new SqlCommand(getEnrollments, this.connection))
            {
                enrollmentResults = command.ExecuteReader();
            }

            while (enrollmentResults.Read())
            {
                EnrollmentDto enrollment = new EnrollmentDto();
                enrollment.Serial = enrollmentResults.GetValue(0).ToString();
                enrollment.Number = Convert.ToInt32(enrollmentResults.GetValue(1));
                int enrollmentId = (int)enrollmentResults.GetValue(2);

                string getVehicle = "SELECT color, engineHorsePower, engineIsStarted FROM vehicle " +
                    "WHERE (enrollmentId=@id)";

                SqlCommand commandVehicle = new SqlCommand(getVehicle, this.connection);
                commandVehicle.Parameters.AddWithValue("@id", enrollmentId);
                SqlDataReader vehicleResults = commandVehicle.ExecuteReader();

                vehicleResults.Read();

                VehicleDto vehicle = new VehicleDto();
                vehicle.Enrollment = enrollment;
                CarColor color;
                Enum.TryParse<CarColor>(vehicleResults.GetValue(0).ToString(), out color);
                vehicle.Color = color;
                EngineDto engine = new EngineDto();
                engine.HorsePower = Convert.ToInt32(vehicleResults.GetValue(1));
                engine.IsStarted = Convert.ToBoolean(vehicleResults.GetValue(2));
                vehicle.Engine = engine;
                string getWheels = "SELECT pressure FROM wheel " +
                    "WHERE (vehicleId=@id)";
                SqlCommand commandWheels = new SqlCommand(getWheels, this.connection);
                commandWheels.Parameters.AddWithValue("@id", enrollmentId);
                SqlDataReader wheelsResults = commandWheels.ExecuteReader();

                List<WheelDto> wheels = new List<WheelDto>();
                while (wheelsResults.Read())
                {
                    WheelDto wheel = new WheelDto();
                    wheel.Pressure = Convert.ToDouble(wheelsResults.GetValue(0));
                    wheels.Add(wheel);
                }
                vehicle.Wheels = wheels.ToArray();

                string getDoors = "SELECT isOpen FROM door " +
                     "WHERE (vehicleId=@id)";
                SqlCommand commandDoors = new SqlCommand(getDoors, this.connection);
                commandDoors.Parameters.AddWithValue("@id", enrollmentId);
                SqlDataReader doorsResults = commandDoors.ExecuteReader();

                List<DoorDto> doors = new List<DoorDto>();
                while (doorsResults.Read())
                {
                    DoorDto door = new DoorDto();
                    door.IsOpen = Convert.ToBoolean(vehicleResults.GetValue(0));
                    doors.Add(door);
                }
                vehicle.Doors = doors.ToArray();

                vehicleCollection.Add(this.vehicleBuilder.import(vehicle));
            }

            return vehicleCollection;
        }

        public void set(IVehicle vehicle)
        {
            string getEnrollments = "SELECT id FROM enrollment " +
              "WHERE (serial = @serial AND number = @number)";

            int enrollmentId;

            using (SqlCommand command = new SqlCommand(getEnrollments, this.connection))
            {
                command.Parameters.AddWithValue("@serial", vehicle.Enrollment.Serial);
                command.Parameters.AddWithValue("@number", vehicle.Enrollment.Number);
                enrollmentId = Convert.ToInt32(command.ExecuteScalar());
            }

            if (enrollmentId > 0)
            {
                //int updatedVehicles = 0;
                //int insertedWheels = 0;
                //int insertedDoors = 0;

                //UPDATE table_name
                //SET column1 = value1, column2 = value2, ...
                //WHERE condition;



                //SqlConnection sqlConnection = new SqlConnection(connectionString);
                //sqlConnection.Open();
                //SqlCommand sqlCommand = new SqlCommand(UPDATE_VEHICLE, sqlConnection);
                //sqlCommand.Parameters.AddWithValue("@enrollmentKEY", enrollmentId);
                //sqlCommand.Parameters.AddWithValue("@color", ((int)vehicle.Color));
                //sqlCommand.Parameters.AddWithValue("@horsepower", vehicle.Engine.HorsePower);
                //sqlCommand.Parameters.AddWithValue("@started", Convert.ToInt32(vehicle.Engine.IsStarted));
                //updatedVehicles = updatedVehicles + sqlCommand.ExecuteNonQuery();


            }
            else
            {
                int insertedVehicles = 0;
                int insertedWheels = 0;
                int insertedDoors = 0;

                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                SqlCommand sqlCommand = new SqlCommand(INSERT_ENROLLMENT, sqlConnection);
                sqlCommand.Parameters.AddWithValue("@serial", vehicle.Enrollment.Serial);
                sqlCommand.Parameters.AddWithValue("@number", vehicle.Enrollment.Number);
                string enrollmentKEY = sqlCommand.ExecuteScalar().ToString();

                sqlCommand = new SqlCommand(INSERT_VEHICLE, sqlConnection);
                sqlCommand.Parameters.AddWithValue("@enrollmentKEY", enrollmentKEY);
                sqlCommand.Parameters.AddWithValue("@color", ((int)vehicle.Color));
                sqlCommand.Parameters.AddWithValue("@horsepower", vehicle.Engine.HorsePower);
                sqlCommand.Parameters.AddWithValue("@started", Convert.ToInt32(vehicle.Engine.IsStarted));
                insertedVehicles = insertedVehicles + sqlCommand.ExecuteNonQuery();

                foreach (IWheel wheel in vehicle.Wheels)
                {
                    sqlCommand = new SqlCommand(INSERT_WHEEL, sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@pressure", wheel.Pressure);
                    sqlCommand.Parameters.AddWithValue("@enrollmentKEY", enrollmentKEY);
                    insertedWheels = insertedWheels + sqlCommand.ExecuteNonQuery();
                }

                foreach (IDoor door in vehicle.Doors)
                {
                    sqlCommand = new SqlCommand(INSERT_DOOR, sqlConnection);
                    sqlCommand.Parameters.AddWithValue("@open", Convert.ToInt32(door.IsOpen));
                    sqlCommand.Parameters.AddWithValue("@enrollmentKEY", enrollmentKEY);
                    insertedDoors = insertedDoors + sqlCommand.ExecuteNonQuery();
                }

                sqlConnection.Close();
            }








        }
    }
}
