#region Important
        /// <summary>
        ///                 INFORMATIOM
        ///  Requirrement Requirrement Requirrement Requirrement         
        ///  Atleast 1 Gyro               
        ///  Atleast 1 remote controll    (find string RemoteControllerName to change it name)
        ///  Atleast 1 landing gear in the button of the ship.                 
        ///  Atleast 1 LCD screen named = "MyAutoScreen"       Or change it in the code!        
        ///  
        ///     
        /// 
        /// 
        /// </summary>


        //Ship speed at attitude
        //[0] = under 2000meter, speed  = 50ms,
        //[1] = under 100 meter speed   =  4ms,
        //[2] = under 3 meter; speed    =  3ms,
        //Ship will automatic disable dampers when  there is less than 1 meter to ground.
        //You can change to wahat you want, this is just my settings
        double[] ShipSpeed = new double[] { 50, 4, 1 };
        double[] ShipAttitude = new double[] { 2000, 100, 3 };

        //Change                       Remote to your Remote controller name. OR change your Remote controller name to = Remote.
        string RemoteControllerName = "Remote";
        string TextPanelName = "MyAutoScreen";
        #endregion;




        #region No touch!
        private List<IMyGyro> Gyros = new List<IMyGyro>();
        private IMyRemoteControl RemoteControl;
        private List<IMyLandingGear> LandingGear = new List<IMyLandingGear>();
        private List<IMyThrust> Thrusters = new List<IMyThrust>();
        private IMyTextPanel TextPanel;
        double CTRL_COEFF = 0.8;



        public Program()
        {
            //Getting all blocks
            try
            {
                TextPanel = GridTerminalSystem.GetBlockWithName(TextPanelName) as IMyTextPanel;
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                GridTerminalSystem.GetBlocksOfType<IMyGyro>(Gyros);
                RemoteControl = GridTerminalSystem.GetBlockWithName(RemoteControllerName) as IMyRemoteControl;
                GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(LandingGear);
                GridTerminalSystem.GetBlocksOfType<IMyThrust>(Thrusters);
            }
            catch (Exception ex)
            {
                if (TextPanel != null)
                    TextPanel.WriteText(ex.Message);
                else
                    Echo(ex.Message);
            }
        }
        public void Main(string argument, UpdateType updateSource)
        {
            var ShipRotation = GetRotation();
            var ShipAngel = GetShipAngel(ShipRotation.Length());
            switch (argument)
            {
                case "AutoLand":
                    StearShip(ShipAngel, ShipRotation);
                    DamperOverride();
                    break;
                case "Program2":
                    //TODO activate programmable block 2, release the hounds
                    break;
            }
        }

        /// <summary>
        /// Controlling the Dampers
        /// </summary>
        private void DamperOverride()
        {
            double PlanetElevation = 0;
            RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out PlanetElevation);
            PlanetElevation = Math.Ceiling(PlanetElevation);
            PlanetElevation -= RemoteControl.CubeGrid.Max.Z;
            LandingGear.ForEach(o => Echo(o.LockMode.ToString()));
            var CurrentSpeed = RemoteControl.GetShipVelocities().LinearVelocity.Length();

            if (RemoteControl.ShowHorizonIndicator && LandingGear.TrueForAll(o => o.LockMode != /*Game.ModAPI.Ingame.*/LandingGearMode.Locked))
            {
                Thrusters.ForEach(o => o.Enabled = true);
                if (PlanetElevation > 2000)
                    ChangeSpeed(0, 0);
                else if (PlanetElevation < ShipAttitude[0] && PlanetElevation > ShipAttitude[1])
                    ChangeSpeed(CurrentSpeed, ShipSpeed[0]);
                else if (PlanetElevation < ShipAttitude[1] && PlanetElevation > ShipAttitude[2])
                    ChangeSpeed(CurrentSpeed, ShipSpeed[1]);
                else if (PlanetElevation < ShipAttitude[2] && PlanetElevation > 1)
                    ChangeSpeed(CurrentSpeed, ShipSpeed[2]);
                else
                    ChangeSpeed(0, 0);
            }
            else
            {
                ChangeSpeed(0, 0);
                Gyros.ForEach(o => o.GyroOverride = false);
                Thrusters.ForEach(o => o.Enabled = false);
            }
        }
        private void ChangeSpeed(double CurrentSpeed, double MaxSpeed)
        {
            if (CurrentSpeed > MaxSpeed)
                RemoteControl.DampenersOverride = true;
            else
                RemoteControl.DampenersOverride = false;
        }
        /// <summary>
        /// Point your ship upwards
        /// </summary>
        /// <param name="ShipAngel"></param>
        /// <param name="ShipRotation"></param>
        private void StearShip(double ShipAngel, Vector3D ShipRotation)
        {
            if (ShipAngel < 0.011)
            {   //Close enough
                Gyros.ForEach(o => o.SetValueBool("Override", false));
            }
            else
            {
                double Vel = Gyros.First().GetMaximum<float>("Yaw") * (ShipAngel / Math.PI) * CTRL_COEFF;
                Vel = Math.Min(Gyros.First().GetMaximum<float>("Yaw"), Vel);
                Vel = Math.Max(0.01, Vel);
                ShipRotation.Normalize();
                ShipRotation *= Vel;
                Gyros.ForEach(o => o.SetValueFloat("Pitch", (float)ShipRotation.GetDim(0)));
                Gyros.ForEach(o => o.SetValueFloat("Yaw", -(float)ShipRotation.GetDim(1)));
                Gyros.ForEach(o => o.SetValueFloat("Roll", -(float)ShipRotation.GetDim(2)));
                Gyros.ForEach(o => o.SetValueFloat("Power", 1.0f));
                Gyros.ForEach(o => o.SetValueBool("Override", true));
            }
        }
        /// <summary>
        /// Get the ship rotation in vector3D
        /// </summary>
        /// <returns></returns>
        private Vector3D GetRotation()
        {
            //Creating object of matrix, At the momment its null.
            Matrix matrix;
            //Getting Remote control Matrix, Parsing it into matrix (I would use Out New Matrix, but SE doesnt allow it )
            RemoteControl.Orientation.GetMatrix(out matrix);
            //Setting the Down Matrix
            Vector3D MatrixDown = matrix.Down;
            //Getting the natural gravity from remote controller
            Vector3D NaturalGravity = RemoteControl.GetNaturalGravity();
            //turns the current vector into a unit vector, 
            NaturalGravity.Normalize();
            //Transforming the 3d vector
            var localDown = Vector3D.Transform(MatrixDown, MatrixD.Transpose(matrix));
            //Transforming the natural gravity.
            var localGrav = Vector3D.Transform(NaturalGravity, MatrixD.Transpose(Gyros.First().WorldMatrix.GetOrientation()));
            //Returning the current ship rotation.
            return Vector3D.Cross(localDown, localGrav);
        }
        /// <summary>
        /// Requirre Rotation.Length
        /// </summary>
        /// <param name="rotation"></param>
        /// <returns></returns>
        private double GetShipAngel(double rotation)
        {
            return Math.Atan2(rotation, Math.Sqrt(Math.Max(0.0, 1.0 - rotation * rotation)));
        }
        #endregion
    
