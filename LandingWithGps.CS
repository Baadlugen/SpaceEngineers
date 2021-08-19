               /// <summary>
        ///                 INFORMATIOM
        ///  Requirrement Requirrement Requirrement Requirrement         
        ///  Atleast 1 Gyro               
        ///  Atleast 1 remote controll    (find string RemoteControllerName to change it name)
        ///  Atleast 1 landing gear in the button of the ship.       https://github.com/Baadlugen/SpaceEngineers/blob/main/LandingWithGps.CS          
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
        private double[] ShipSpeed = new double[] { 50, 4, 1 };
        private double[] ShipAttitude = new double[] { 2000, 100, 3 };
        //Change                       Remote to your Remote controller name. OR change your Remote controller name to = Remote.
        private string RemoteControllerName = "Remote";
        private string TextPanelName = "MyAutoScreen";



        private List<IMyGyro> Gyros = new List<IMyGyro>();
        private IMyRemoteControl RemoteControl;
        private List<IMyLandingGear> LandingGear = new List<IMyLandingGear>();
        private List<IMyThrust> Thrusters = new List<IMyThrust>();
        private IMyTextPanel TextPanel;
        private double CTRL_COEFF = 0.8;
        public TimeSpan waitSpan = new TimeSpan(0, 0, 0);
        private Mode CurrentMode = Mode.FirstStart;

        public Program()
        {
            //Getting all blocks
            try
            {
                TextPanel = GridTerminalSystem.GetBlockWithName(TextPanelName) as IMyTextPanel;
                GridTerminalSystem.GetBlocksOfType<IMyGyro>(Gyros);
                RemoteControl = GridTerminalSystem.GetBlockWithName(RemoteControllerName) as IMyRemoteControl;
                GridTerminalSystem.GetBlocksOfType<IMyLandingGear>(LandingGear);
                GridTerminalSystem.GetBlocksOfType<IMyThrust>(Thrusters);
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            }
            catch (Exception ex)
            {
                if (TextPanel != null)
                    TextPanel.WriteText(ex.Message);
                else
                    Echo(ex.Message);
            }
        }

        int counter = 0;
        public void Main(string argument)
        {

            Log(CurrentMode.ToString(), true);
            switch (CurrentMode)
            {
                case Mode.FirstStart:
                    CurrentMode = TurnShipOn();
                    break;
                case Mode.SetAutoPilot:
                    CurrentMode = SetAutoPilot();
                    break;
                case Mode.GoingToGps:
                    CurrentMode = GoToGps(ControllSpeedReturnDistance());
                    break;
                case Mode.ReverseGps:
                    CurrentMode = ReverseGPS();
                    break;

                case Mode.Landing:
                    CurrentMode = LandShipOnPlanet();
                    break;
                case Mode.TurnShipOff:
                    CurrentMode = TurnShipOff();
                    break;
                case Mode.TurnShipOn:
                    CurrentMode = SetAutoPilot();
                    break;
                case Mode.Idle:
                    Echo(counter.ToString());
                    if (counter > 200)
                    {
                        counter = 0;
                        CurrentMode = Mode.FirstStart;
                    }
                    counter++;
                    break;

            }

        }
        /// <summary>
        /// GoingToGps or Landing;
        /// </summary>
        /// <returns></returns>
        private Mode GoToGps(double distance)
        {
            List<MyWaypointInfo> Waypoints = new List<MyWaypointInfo>();
            RemoteControl.GetWaypointInfo(Waypoints);
            if (distance > 2)
                return Mode.GoingToGps;
            return Mode.Landing;
        }

        /// <summary>
        /// return ReverseGps or GoingToGps
        /// </summary>
        /// <returns></returns>
        private Mode SetAutoPilot()
        {
            List<MyWaypointInfo> Waypoints = new List<MyWaypointInfo>();
            MyWaypointInfo CurrentWayPoint = RemoteControl.CurrentWaypoint;
            RemoteControl.GetWaypointInfo(Waypoints);
            if (CurrentWayPoint.Name == Waypoints.Last().Name)
            {
                return Mode.ReverseGps;
            }
            LandingGear.ForEach(o => ((IMyTerminalBlock)o).ApplyAction("Unlock"));
            RemoteControl.SetAutoPilotEnabled(true);
            return Mode.GoingToGps;

        }
        /// <summary>
        /// GotoGps or Idle
        /// </summary>
        /// <returns></returns>
        private Mode ReverseGPS()
        {
            try
            {
                List<MyWaypointInfo> Waypoints = new List<MyWaypointInfo>();
                RemoteControl.GetWaypointInfo(Waypoints);
                RemoteControl.ClearWaypoints();
                Waypoints.Reverse();
                Waypoints.ForEach(o => RemoteControl.AddWaypoint(o));
                return Mode.SetAutoPilot;

            }
            catch (Exception ex)
            {
                Log(ex.Message, false);
                return Mode.Idle;
            }
        }
        private void ControlShipSpeedLimit(double DistanceToLastWayPoint)
        {
            if (DistanceToLastWayPoint > 999)
                RemoteControl.SpeedLimit = 100;
            else if (DistanceToLastWayPoint > 499 && DistanceToLastWayPoint < 1000)
                RemoteControl.SpeedLimit = 40;
            else if (DistanceToLastWayPoint > 49 && DistanceToLastWayPoint < 500)
                RemoteControl.SpeedLimit = 10;
            else if (DistanceToLastWayPoint > 4 && DistanceToLastWayPoint < 50)
                RemoteControl.SpeedLimit = 5;
            else if (DistanceToLastWayPoint < 5 && DistanceToLastWayPoint > 2)
                RemoteControl.SpeedLimit = 2;
            if (DistanceToLastWayPoint < 2)
                RemoteControl.SpeedLimit = 100;
        }
        /// <summary>
        /// Return Idle if break Or SetAutoPilot
        /// </summary>
        /// <returns></returns>
        private Mode TurnShipOn()
        {
            try
            {
                Gyros.ForEach(o => o.Enabled = true);
                Gyros.ForEach(o => o.GyroOverride = true);
                Thrusters.ForEach(o => o.Enabled = true);
                DampShip(1, 0);
                Log("Ship Started", true);
                return Mode.SetAutoPilot;
            }
            catch (Exception ex)
            {
                Log(ex.Message, false);
                return Mode.Idle;
            }

        }
        /// <summary>
        /// Return Idle
        /// </summary>
        /// <returns></returns>
        private Mode TurnShipOff()
        {
            Gyros.ForEach(o => o.GyroOverride = false);
            Thrusters.ForEach(o => o.Enabled = false);
            DampShip(0, 0);
            return Mode.Idle;
        }
        /// <summary>
        /// Return landing or TurnShipOff
        /// </summary>
        /// <returns></returns>
        private Mode LandShipOnPlanet()
        {
            if (RemoteControl.ShowHorizonIndicator && LandingGear.TrueForAll(o => o.LockMode != LandingGearMode.Locked))
            {
                var shipRotation = GetShipRotation();
                TurnShipVertical(GetShipAngel(shipRotation.Length()), shipRotation);
                double PlanetElevation = 0;
                RemoteControl.TryGetPlanetElevation(MyPlanetElevation.Surface, out PlanetElevation);
                PlanetElevation = Math.Ceiling(PlanetElevation);
                PlanetElevation -= RemoteControl.CubeGrid.Max.Z;
                LandingGear.ForEach(o => Echo(o.LockMode.ToString()));
                var CurrentSpeed = RemoteControl.GetShipVelocities().LinearVelocity.Length();
                Thrusters.ForEach(o => o.Enabled = true);
                if (PlanetElevation > 2000)
                    DampShip(0, 0);
                else if (PlanetElevation < ShipAttitude[0] && PlanetElevation > ShipAttitude[1])
                    DampShip(CurrentSpeed, ShipSpeed[0]);
                else if (PlanetElevation < ShipAttitude[1] && PlanetElevation > ShipAttitude[2])
                    DampShip(CurrentSpeed, ShipSpeed[1]);
                else if (PlanetElevation < ShipAttitude[2] && PlanetElevation > 1)
                    DampShip(CurrentSpeed, ShipSpeed[2]);
                else
                    DampShip(0, 0);
                return Mode.Landing;
            }
            return Mode.TurnShipOff;
        }

        #region Functions
        private void DampShip(double CurrentSpeed, double MaxSpeed)
        {
            if (CurrentSpeed > MaxSpeed)
                RemoteControl.DampenersOverride = true;
            else
                RemoteControl.DampenersOverride = false;
        }
        private Vector3D GetShipRotation()
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
        private double GetShipAngel(double rotation)
        {
            return Math.Atan2(rotation, Math.Sqrt(Math.Max(0.0, 1.0 - rotation * rotation)));
        }
        private void TurnShipVertical(double ShipAngel, Vector3D ShipRotation)
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
        private void Log(string message, bool append)
        {
            if (TextPanel == null)
                Echo(message);
            else
                TextPanel.WriteText(message);
        }
        private double ControllSpeedReturnDistance()
        {
            try
            {
                List<MyWaypointInfo> LastCordinate = new List<MyWaypointInfo>();
                RemoteControl.GetWaypointInfo(LastCordinate);
                var distance = Vector3D.Distance(RemoteControl.GetPosition(), LastCordinate.Last().Coords);
                try
                {

                    double? arrivaltime = (distance / RemoteControl.GetShipSpeed());
                    if (arrivaltime != null)
                    {
                        
                        Log($"Arrival time : {(int)arrivaltime} secounds", true);
                    }

                }
                catch (Exception)
                {


                }
                ControlShipSpeedLimit(distance);
                return distance;
            }
            catch (Exception ex)
            {

                Log(ex.Message, false);
                return 0;
            }
        }
        private enum Mode
        {
            GoingToGps,
            Landing,
            TurnShipOff,
            TurnShipOn,
            Idle,
            FirstStart,
            ReverseGps,
            SetAutoPilot,
        }
        #endregion
