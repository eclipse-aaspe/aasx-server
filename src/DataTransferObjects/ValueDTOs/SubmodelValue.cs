﻿namespace DataTransferObjects.ValueDTOs
{
    public record class SubmodelValue(List<ISubmodelElementValue>? submodelElements = null) : IValueDTO;
}
