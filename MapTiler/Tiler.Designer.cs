namespace MapTiler
{
    partial class Tiler
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Tiler));
            this.MainMap = new GMap.NET.WindowsForms.GMapControl();
            this.button1 = new System.Windows.Forms.Button();
            this.status = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // MainMap
            // 
            this.MainMap.Bearing = 0F;
            this.MainMap.CanDragMap = true;
            this.MainMap.GrayScaleMode = false;
            this.MainMap.LevelsKeepInMemmory = 5;
            this.MainMap.Location = new System.Drawing.Point(24, 13);
            this.MainMap.MarkersEnabled = true;
            this.MainMap.MaxZoom = 17;
            this.MainMap.MinZoom = 2;
            this.MainMap.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            this.MainMap.Name = "MainMap";
            this.MainMap.PolygonsEnabled = true;
            this.MainMap.Position = ((GMap.NET.PointLatLng)(resources.GetObject("MainMap.Position")));
            this.MainMap.RetryLoadTile = 0;
            this.MainMap.RoutesEnabled = true;
            this.MainMap.ShowTileGridLines = false;
            this.MainMap.Size = new System.Drawing.Size(730, 490);
            this.MainMap.TabIndex = 0;
            this.MainMap.Zoom = 0D;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(701, 520);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(71, 30);
            this.button1.TabIndex = 1;
            this.button1.Text = "Stitch Map";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // status
            // 
            this.status.AutoSize = true;
            this.status.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.status.Location = new System.Drawing.Point(20, 524);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(0, 20);
            this.status.TabIndex = 2;
            // 
            // Tiler
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 562);
            this.Controls.Add(this.status);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.MainMap);
            this.Name = "Tiler";
            this.Text = "Map Tiler";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private GMap.NET.WindowsForms.GMapControl MainMap;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label status;
    }
}

