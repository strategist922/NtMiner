﻿using System;

namespace NTMiner.Core.Kernels.Impl {
    public class KernelOutputPickerData : IKernelOutputPicker, IDbEntity<Guid> {
        public KernelOutputPickerData() {
        }

        public Guid GetId() {
            return this.Id;
        }

        public Guid Id { get; set; }

        public string TotalSpeedPattern { get; set; }

        public string TotalSharePattern { get; set; }

        public string AcceptSharePattern { get; set; }

        public string RejectSharePattern { get; set; }

        public string RejectPercentPattern { get; set; }

        public string GpuSpeedPattern { get; set; }


        public string DualTotalSpeedPattern { get; set; }

        public string DualTotalSharePattern { get; set; }

        public string DualAcceptSharePattern { get; set; }

        public string DualRejectSharePattern { get; set; }

        public string DualRejectPercentPattern { get; set; }

        public string DualGpuSpeedPattern { get; set; }
    }
}
