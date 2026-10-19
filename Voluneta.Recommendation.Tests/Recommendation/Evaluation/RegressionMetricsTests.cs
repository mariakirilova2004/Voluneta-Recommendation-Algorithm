using System;
using System.Collections.Generic;
using System.Text;
using Voluneta.Recommendation.Core.Algorithms.Recommendation.Evaluation;

namespace Voluneta.Recommendation.Tests.Recommendation.Evaluation
{
    public class RegressionMetricsTests
    {
        [Fact]
        public void MAE()
        {
            // Arrange
            double[] actual = { 0.2, 0.5, 0.8 };
            double[] predicted = { 0.3, 0.4, 0.6 };

            // Act
            double mae = RegressionMetrics.MAE(actual, predicted);

            // Assert
            Assert.Equal(0.1333333333, mae, 8);
        }

        [Fact]
        public void RMSE()
        {
            // Arrange
            double[] actual = { 0.2, 0.5, 0.8 };
            double[] predicted = { 0.3, 0.4, 0.6 };

            // Act
            double rmse = RegressionMetrics.RMSE(actual, predicted);

            // Assert
            Assert.Equal(Math.Sqrt(0.02), rmse, 10);
        }

        [Fact]
        public void Metrics()
        {
            double[] actual = { 0.1, 0.5, 0.9 };
            double[] predicted = { 0.1, 0.5, 0.9 };

            Assert.Equal(0.0, RegressionMetrics.MAE(actual, predicted));
            Assert.Equal(0.0, RegressionMetrics.RMSE(actual, predicted));
        }

        [Fact]
        public void ToRatingScale_Converts()
        {
            // Arrange
            double normalizedError = 0.1;

            // Act
            double ratingError = RegressionMetrics.ToRatingScale(normalizedError);

            // Assert
            Assert.Equal(0.9, ratingError, 10);
        }

        [Fact]
        public void Metrics_DifferentLengths()
        {
            double[] actual = { 0.2, 0.5 };
            double[] predicted = { 0.3 };

            Assert.Throws<ArgumentException>(() => RegressionMetrics.MAE(actual, predicted));

            Assert.Throws<ArgumentException>(() => RegressionMetrics.RMSE(actual, predicted));
        }

        [Fact]
        public void Metrics_EmptyCollections()
        {
            double[] actual = Array.Empty<double>();
            double[] predicted = Array.Empty<double>();

            Assert.Throws<ArgumentException>(() => RegressionMetrics.MAE(actual, predicted));

            Assert.Throws<ArgumentException>(() => RegressionMetrics.RMSE(actual, predicted));
        }

        [Fact]
        public void Metrics_NonFinite()
        {
            double[] actual = { 0.2, double.NaN };
            double[] predicted = { 0.3, 0.4 };

            Assert.Throws<ArgumentException>(() => RegressionMetrics.MAE(actual, predicted));

            Assert.Throws<ArgumentException>(() => RegressionMetrics.RMSE(actual, predicted));
        }

        [Fact]
        public void ToRatingScale_WithNegativeError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RegressionMetrics.ToRatingScale(-0.1));
        }
    }
}
